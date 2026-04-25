using UnityEngine;
using UnityEngine.InputSystem;
using TerrasHeart.Events;

namespace TerrasHeart.Player
{
    /// <summary>
    /// Controls Dr. Maria's movement, jump, slide, and swim states.
    /// Uses New Input System (Keyboard.current polling) exclusively.
    /// Cross-system communication via GameEvents bus only.
    ///
    /// State priority (highest to lowest): Swim > Slide > Land
    /// Swim entry/exit driven by WaterVolume → GameEvents.OnWaterEntered / OnWaterExited.
    /// Slide entry driven by contact normal + SlipperySlope physics material detection.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        // SERIALIZED FIELDS
        // ─────────────────────────────────────────────────────────────

        [Header("Movement")]
        [Tooltip("Horizontal move speed on land (units/s).")]
        [SerializeField] private float _moveSpeed = 5f;

        [Tooltip("Vertical launch force applied on jump.")]
        [SerializeField] private float _jumpForce = 10f;

        [Header("Swim")]
        [SerializeField] private SwimConfigSO _swimConfig;

        [Header("Slide")]
        [Tooltip("Deceleration rate applied once the slide reaches flat ground (Mathf.MoveTowards).")]
        [SerializeField] private float _slideDecelerationRate = 8f;

        [Header("Ground Detection")]
        [Tooltip("Ground physics layer. Set to the 'Ground' layer in the Inspector.")]
        [SerializeField] private LayerMask _groundLayer;

        // ─────────────────────────────────────────────────────────────
        // PRIVATE STATE
        // ─────────────────────────────────────────────────────────────

        private Rigidbody2D _rb;

        // Grounded
        private bool _isGrounded;

        // Slide
        private bool _isSliding;
        private bool _isOnSlipperySurface; // set by OnCollisionStay2D each physics step

        // Swim
        private bool _isSwimming;
        private float _waterSurfaceY;
        private float _originalGravityScale;

        // Jump buffering (captured in Update, consumed in FixedUpdate)
        private bool _jumpRequested;

        // ─────────────────────────────────────────────────────────────
        // PUBLIC ACCESSORS
        // ─────────────────────────────────────────────────────────────

        /// <summary>Whether Dr. Maria is currently touching the ground layer.</summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>Whether Dr. Maria is currently in swim state.</summary>
        public bool IsSwimming => _isSwimming;

        /// <summary>Whether Dr. Maria is currently in slide state.</summary>
        public bool IsSliding => _isSliding;

        // ─────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _originalGravityScale = _rb.gravityScale;
        }

        private void OnEnable()
        {
            GameEvents.OnWaterEntered += HandleWaterEntered;
            GameEvents.OnWaterExited += HandleWaterExited;
        }

        private void OnDisable()
        {
            GameEvents.OnWaterEntered -= HandleWaterEntered;
            GameEvents.OnWaterExited -= HandleWaterExited;
        }

        // ─────────────────────────────────────────────────────────────
        // UPDATE — input capture only
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Jump buffering — captured here, consumed in FixedUpdate
            if (keyboard.spaceKey.wasPressedThisFrame)
                _jumpRequested = true;

            // Palette input — raise immediately on press
            if (keyboard.digit1Key.wasPressedThisFrame) GameEvents.RaisePaletteInput(0); // Cyan
            if (keyboard.digit2Key.wasPressedThisFrame) GameEvents.RaisePaletteInput(1); // Amber

            // Throw input — raise immediately, ThrowController consumes
            if (keyboard.tKey.wasPressedThisFrame) GameEvents.RaiseThrowInput();
        }

        // ─────────────────────────────────────────────────────────────
        // FIXED UPDATE — movement execution
        // ─────────────────────────────────────────────────────────────

        private void FixedUpdate()
        {
            _isGrounded = _rb.IsTouchingLayers(_groundLayer);

            if (_isSwimming)
            {
                HandleSwimMovement();
                CheckSwimGroundExit();
            }
            else if (_isSliding)
            {
                HandleSlideMovement();
            }
            else
            {
                HandleLandMovement();
            }

            _jumpRequested = false;
            _isOnSlipperySurface = false;
        }

        // ─────────────────────────────────────────────────────────────
        // LAND MOVEMENT
        // ─────────────────────────────────────────────────────────────

        private void HandleLandMovement()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float horizontal = 0f;
            if (keyboard.aKey.isPressed) horizontal = -1f;
            if (keyboard.dKey.isPressed) horizontal = 1f;

            _rb.linearVelocity = new Vector2(horizontal * _moveSpeed, _rb.linearVelocity.y);

            if (_jumpRequested && _isGrounded)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
                _jumpRequested = false;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // SWIM MOVEMENT
        // ─────────────────────────────────────────────────────────────

        private void HandleSwimMovement()
        {
            if (_swimConfig == null)
            {
                Debug.LogWarning("[PlayerController] SwimConfigSO not assigned. Assign it in the Inspector.", this);
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float horizontal = 0f;
            if (keyboard.aKey.isPressed) horizontal = -1f;
            if (keyboard.dKey.isPressed) horizontal = 1f;

            float vertical;
            if (keyboard.wKey.isPressed)
                vertical = _swimConfig.UpSpeed;
            else if (keyboard.sKey.isPressed)
                vertical = -_swimConfig.DownSpeed;
            else
                vertical = _swimConfig.BuoyancyForce;

            _rb.linearVelocity = new Vector2(horizontal * _swimConfig.HorizontalSpeed, vertical);

            _jumpRequested = false;
        }

        private void CheckSwimGroundExit()
        {
            if (_isGrounded && transform.position.y >= _waterSurfaceY)
                ExitSwimState();
        }

        // ─────────────────────────────────────────────────────────────
        // SLIDE MOVEMENT
        // ─────────────────────────────────────────────────────────────

        private void HandleSlideMovement()
        {
            if (_isOnSlipperySurface) return;

            if (_isGrounded)
            {
                float deceleratedX = Mathf.MoveTowards(
                    _rb.linearVelocity.x, 0f, _slideDecelerationRate * Time.fixedDeltaTime);
                _rb.linearVelocity = new Vector2(deceleratedX, _rb.linearVelocity.y);

                if (Mathf.Abs(_rb.linearVelocity.x) < 0.05f)
                    ExitSlideState();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // COLLISION CALLBACKS — slide detection
        // ─────────────────────────────────────────────────────────────

        private void OnCollisionStay2D(Collision2D col)
        {
            foreach (var contact in col.contacts)
            {
                bool isSteep = contact.normal.y > 0.1f && contact.normal.y < 0.7f;
                bool isSlippery = col.collider.sharedMaterial != null
                                  && col.collider.sharedMaterial.friction <= 0f;

                if (isSteep && isSlippery)
                {
                    _isOnSlipperySurface = true;

                    if (!_isSliding && !_isSwimming)
                        EnterSlideState();

                    return;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // SLIDE STATE
        // ─────────────────────────────────────────────────────────────

        private void EnterSlideState()
        {
            _isSliding = true;
            GameEvents.RaiseSlideStateChanged(true);
        }

        private void ExitSlideState()
        {
            _isSliding = false;
            GameEvents.RaiseSlideStateChanged(false);
        }

        // ─────────────────────────────────────────────────────────────
        // SWIM STATE
        // ─────────────────────────────────────────────────────────────

        private void HandleWaterEntered(float surfaceY)
        {
            _waterSurfaceY = surfaceY;
            EnterSwimState();
        }

        private void HandleWaterExited()
        {
            ExitSwimState();
        }

        private void EnterSwimState()
        {
            if (_isSliding) ExitSlideState();

            _isSwimming = true;
            _rb.gravityScale = 0f;
            GameEvents.RaiseSwimStateChanged(true);
        }

        private void ExitSwimState()
        {
            _isSwimming = false;
            _rb.gravityScale = _originalGravityScale;
            GameEvents.RaiseSwimStateChanged(false);
        }
    }
}