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
    /// Swim entry/exit driven by WaterSubmersionController → GameEvents.OnPlayerSubmerged.
    /// WaterSubmersionController handles gravity/damping changes — PlayerController handles input only.
    /// Slide entry: sharedMaterial.name == _slopeMaterialName AND steep contact normal.
    /// Slide exit: flat contact normal (angle from Vector2.up <= _flatGroundAngleThreshold).
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

        [Header("Jump")]
        [Tooltip("Vertical launch force applied on a normal jump.")]
        [SerializeField] private float _jumpForce = 10f;

        [Header("Swim")]
        [SerializeField] private SwimConfigSO _swimConfig;

        [Header("Slide State")]
        [Tooltip("PhysicsMaterial2D name that triggers slide on contact.")]
        [SerializeField] private string _slopeMaterialName = "SlipperySlope";

        [Tooltip("Max angle from Vector2.up still considered flat ground (degrees). " +
                 "Contact normals within this angle exit the slide state.")]
        [SerializeField] private float _flatGroundAngleThreshold = 20f;

        [Tooltip("Deceleration rate (units/s) applied via MoveTowards after slide exits.")]
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

        // Jump
        private bool _jumpRequested;

        // Slide
        private bool _isSliding;
        private bool _isDecelerating;

        // Swim — gravity/damping managed by WaterSubmersionController
        private bool _isSwimming;

        // ─────────────────────────────────────────────────────────────
        // PUBLIC ACCESSORS
        // ─────────────────────────────────────────────────────────────

        /// <summary>Whether Dr. Maria is currently touching the ground layer.</summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>Whether Dr. Maria is currently in slide state.</summary>
        public bool IsSliding => _isSliding;

        /// <summary>Whether Dr. Maria is currently in swim state.</summary>
        public bool IsSwimming => _isSwimming;

        // ─────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerSubmerged += HandlePlayerSubmerged;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerSubmerged -= HandlePlayerSubmerged;
        }

        // ─────────────────────────────────────────────────────────────
        // UPDATE — input capture only
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Jump buffering — captured here, consumed in FixedUpdate
            // RaiseJumpInput fires immediately so JumpPad can cancel before FixedUpdate runs
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _jumpRequested = true;
                GameEvents.RaiseJumpInput();
            }

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
            }
            else if (_isSliding)
            {
                // Slide: physics owns the descent — no velocity override
            }
            else if (_isDecelerating)
            {
                HandleDeceleration();
            }
            else
            {
                HandleLandMovement();
            }

            _jumpRequested = false;
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

            ApplyJump();
        }

        // ─────────────────────────────────────────────────────────────
        // JUMP
        // ─────────────────────────────────────────────────────────────

        private void ApplyJump()
        {
            if (!_jumpRequested || !_isGrounded) return;

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
            _jumpRequested = false;
        }

        /// <summary>
        /// Called by JumpPad to cancel the buffered normal jump before FixedUpdate applies it.
        /// JumpPad applies its own boosted launch force instead.
        /// </summary>
        public void CancelJumpRequest() => _jumpRequested = false;

        // ─────────────────────────────────────────────────────────────
        // SWIM MOVEMENT
        // WaterSubmersionController owns gravity and damping.
        // PlayerController only overrides velocity for directional input.
        // ─────────────────────────────────────────────────────────────

        private void HandleSwimMovement()
        {
            if (_swimConfig == null)
            {
                Debug.LogWarning("[PlayerController] SwimConfigSO not assigned.", this);
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

        // ─────────────────────────────────────────────────────────────
        // DECELERATION — post-slide momentum bleed-off
        // ─────────────────────────────────────────────────────────────

        private void HandleDeceleration()
        {
            var keyboard = Keyboard.current;
            float inputX = 0f;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed) inputX = -1f;
                if (keyboard.dKey.isPressed) inputX = 1f;
            }

            float targetX = inputX * _moveSpeed;
            float newX = Mathf.MoveTowards(
                _rb.linearVelocity.x, targetX, _slideDecelerationRate * Time.fixedDeltaTime);

            _rb.linearVelocity = new Vector2(newX, _rb.linearVelocity.y);

            if (Mathf.Approximately(newX, targetX))
                _isDecelerating = false;
        }

        // ─────────────────────────────────────────────────────────────
        // COLLISION CALLBACKS — slide entry and exit
        // ─────────────────────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (_isSwimming) return;
            TryEnterSlide(col);
            TryExitSlide(col);
        }

        private void OnCollisionStay2D(Collision2D col)
        {
            if (_isSwimming) return;
            TryEnterSlide(col);
            TryExitSlide(col);
        }

        // ─────────────────────────────────────────────────────────────
        // SLIDE ENTRY — material name + steep normal
        // ─────────────────────────────────────────────────────────────

        private void TryEnterSlide(Collision2D col)
        {
            if (_isSliding) return;

            if (col.collider.sharedMaterial == null) return;
            if (col.collider.sharedMaterial.name != _slopeMaterialName) return;

            foreach (var contact in col.contacts)
            {
                float angle = Vector2.Angle(contact.normal, Vector2.up);
                if (angle > _flatGroundAngleThreshold)
                {
                    EnterSlideState();
                    return;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // SLIDE EXIT — flat ground normal (material-agnostic)
        // ─────────────────────────────────────────────────────────────

        private void TryExitSlide(Collision2D col)
        {
            if (!_isSliding) return;

            foreach (var contact in col.contacts)
            {
                float angle = Vector2.Angle(contact.normal, Vector2.up);
                if (angle <= _flatGroundAngleThreshold)
                {
                    ExitSlideState();
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
            _isDecelerating = false;
            GameEvents.RaiseSlideStateChanged(true);
        }

        private void ExitSlideState()
        {
            _isSliding = false;
            _isDecelerating = true;
            GameEvents.RaiseSlideStateChanged(false);
        }

        // ─────────────────────────────────────────────────────────────
        // SWIM STATE
        // Gravity and damping owned by WaterSubmersionController.
        // PlayerController only toggles input override.
        // ─────────────────────────────────────────────────────────────

        private void HandlePlayerSubmerged(bool isSubmerged)
        {
            if (isSubmerged)
                EnterSwimState();
            else
                ExitSwimState();
        }

        private void EnterSwimState()
        {
            if (_isSliding) ExitSlideState();

            _isSwimming = true;
            _isDecelerating = false;
            GameEvents.RaiseSwimStateChanged(true);
        }

        private void ExitSwimState()
        {
            _isSwimming = false;
            GameEvents.RaiseSwimStateChanged(false);
        }
    }
}