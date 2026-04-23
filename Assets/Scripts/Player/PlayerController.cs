// ─────────────────────────────────────────────────────────────────────────────
// PlayerController.cs
// Path: Assets/Scripts/Player/PlayerController.cs
// Terra's Heart — Dr. Maria's movement controller.
//
// Updated:
//   - Wall-stick fix: CheckGround() now uses a two-pass approach.
//     Pass 1: OverlapCircle confirms something on the Ground layer is in range.
//     Pass 2: rb.GetContacts() verifies at least one contact has a normal within
//     _groundContactAngleMax degrees of Vector2.up. Wall contacts (~90°) are
//     rejected; floor and slope contacts (0°–60°) are accepted. Prevents wall
//     touch from setting _isGrounded = true and blocking jump input.
//     Physics Material 2D (Friction 0, Bounciness 0) must also be applied to
//     Tilemap_Walls and Tilemap_Ceiling in the scene to eliminate the friction
//     sticking — the code fix alone is not sufficient.
//   - Added slow walk (Left Shift held) — reduces move speed below creature
//     detection threshold. Tune _slowWalkSpeed to stay below
//     CaveLuminothAI._slowSpeedThreshold (currently 1.5).
//   - Added throw input (T key) — fires GameEvents.RaiseThrowInput().
//   - Phase B Step 4: Added palette input (1, 2 keys) — fires
//     GameEvents.RaisePaletteInput(index) for Glow-Mantle call-and-response.
//     Always fires on key press — GlowMantleAI filters by state.
//   - BrineglowDescent: Added slide state — triggered on contact with a surface
//     using the SlipperySlope PhysicsMaterial2D. All player input suppressed
//     during slide; physics owns the descent completely. Exits when contact
//     normal returns to near-vertical (flat ground). Fires
//     GameEvents.RaiseSlideStateChanged(bool). Animation hookup is production phase.
//   - BrineglowDescent: Replaced momentum frame skip with smooth deceleration.
//     After slide exit, horizontal velocity bleeds off toward input-driven target
//     via Mathf.MoveTowards at _slideDecelerationRate units/s. No input = smooth
//     coast to zero. Input pressed mid-deceleration = blends into movement speed.
//
// ⚠ AFTER REPLACING THIS SCRIPT re-assign in Inspector:
//   - Ground Check  → drag GroundCheck child transform
//   - Ground Layer  → select Ground layer
//   - Adaptation Manager → drag AdaptationManager component on DrMaria
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;
using TerrasHeart.Adaptations;
using TerrasHeart.Events;

namespace TerrasHeart.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;

        [Tooltip("Speed when Left Shift is held. Must stay below CaveLuminothAI " +
                 "Slow Speed Threshold (default 1.5) to avoid detection.")]
        [SerializeField] private float _slowWalkSpeed = 1.2f;

        [SerializeField] private float _jumpForce = 10f;

        [Header("Ground Check")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundCheckRadius = 0.1f;
        [SerializeField] private LayerMask _groundLayer;

        [Tooltip("Maximum angle (degrees) from Vector2.up for a contact normal to " +
                 "count as ground. Wall contacts are ~90° and will be rejected. " +
                 "Floor contacts are ~0° and will be accepted. Default 60° accepts " +
                 "floors and moderate slopes while filtering all wall contacts.")]
        [SerializeField] private float _groundContactAngleMax = 60f;

        [Header("Adaptations")]
        [Tooltip("Assign the AdaptationManager component on DrMaria.")]
        [SerializeField] private AdaptationManager _adaptationManager;

        [Header("Slide State")]
        [Tooltip("Maximum angle (degrees) from Vector2.up that is still considered " +
                 "flat ground. Contacts above this angle are treated as slope. " +
                 "Tune against the actual BrineglowDescent slope geometry.")]
        [SerializeField] private float _flatGroundAngleThreshold = 20f;

        [Tooltip("Exact name of the PhysicsMaterial2D that triggers the slide state.")]
        [SerializeField] private string _slopeMaterialName = "SlipperySlope";

        [Tooltip("Rate at which horizontal velocity bleeds off after slide exit " +
                 "(units per second). Higher = stops faster. Lower = longer coast. " +
                 "Also controls how quickly input blends in mid-deceleration.")]
        [SerializeField] private float _slideDecelerationRate = 8f;

        // ─── Private State ────────────────────────────────────────────────────

        private Rigidbody2D _rb;
        private bool _isGrounded;
        private Vector2 _moveInput;
        private bool _isSlowWalking;
        private bool _isSliding;
        private bool _isDecelerating;

        // Reusable contact buffer — avoids per-frame allocation in CheckGround().
        private readonly ContactPoint2D[] _contactBuffer = new ContactPoint2D[16];

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            // All input suppressed during slide — physics owns the descent.
            if (_isSliding) return;

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            HandleMovementInput(kb);
            CheckGround();
            HandleJumpInput(kb);
            HandleThrowInput(kb);
            HandlePaletteInput(kb);
            FlipSprite();
        }

        private void FixedUpdate()
        {
            // Physics drives the slide completely — no velocity assignment here.
            if (_isSliding) return;

            float speed = _isSlowWalking
                ? _slowWalkSpeed
                : _moveSpeed + GetMoveSpeedBonus();

            float targetVelocityX = _moveInput.x * speed;

            if (_isDecelerating)
            {
                // Bleed horizontal velocity toward input-driven target.
                // No input → target is 0, smooth coast to stop.
                // Input pressed → target is ±speed, blends from momentum into movement.
                float newVelocityX = Mathf.MoveTowards(
                    _rb.linearVelocity.x,
                    targetVelocityX,
                    _slideDecelerationRate * Time.fixedDeltaTime);

                _rb.linearVelocity = new Vector2(newVelocityX, _rb.linearVelocity.y);

                // Exit deceleration once velocity has reached the target.
                if (newVelocityX == targetVelocityX)
                    _isDecelerating = false;
            }
            else
            {
                _rb.linearVelocity = new Vector2(targetVelocityX, _rb.linearVelocity.y);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Input
        // ─────────────────────────────────────────────────────────────────────

        private void HandleMovementInput(Keyboard kb)
        {
            float horizontal = 0f;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) horizontal = -1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) horizontal = 1f;
            _moveInput = new Vector2(horizontal, 0f);

            _isSlowWalking = kb.leftShiftKey.isPressed;
        }

        private void HandleJumpInput(Keyboard kb)
        {
            if ((kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                && _isGrounded)
            {
                float totalJumpForce = _jumpForce + GetJumpBonus();
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, totalJumpForce);
            }
        }

        private void HandleThrowInput(Keyboard kb)
        {
            if (kb.tKey.wasPressedThisFrame)
                GameEvents.RaiseThrowInput();
        }

        private void HandlePaletteInput(Keyboard kb)
        {
            // 1 = Cyan (index 0), 2 = Amber (index 1).
            // Always fires — GlowMantleAI filters by encounter state.
            if (kb.digit1Key.wasPressedThisFrame) GameEvents.RaisePaletteInput(0);
            if (kb.digit2Key.wasPressedThisFrame) GameEvents.RaisePaletteInput(1);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Ground Check
        // ─────────────────────────────────────────────────────────────────────

        private void CheckGround()
        {
            // Pass 1 — broad spatial check: is anything on the Ground layer nearby?
            bool overlap = Physics2D.OverlapCircle(
                _groundCheck.position, _groundCheckRadius, _groundLayer);

            if (!overlap)
            {
                _isGrounded = false;
                return;
            }

            // Pass 2 — normal filter: is any active contact coming from below?
            // Wall contacts have a normal ~90° from Vector2.up and will be rejected.
            // Floor and slope contacts are within _groundContactAngleMax and accepted.
            // This prevents pressing against a wall from setting _isGrounded = true.
            int count = _rb.GetContacts(_contactBuffer);
            for (int i = 0; i < count; i++)
            {
                float angle = Vector2.Angle(_contactBuffer[i].normal, Vector2.up);
                if (angle < _groundContactAngleMax)
                {
                    _isGrounded = true;
                    return;
                }
            }

            _isGrounded = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Sprite Flip
        // ─────────────────────────────────────────────────────────────────────

        private void FlipSprite()
        {
            if (_moveInput.x > 0f) transform.localScale = new Vector3(0.5f, 1f, 1f);
            if (_moveInput.x < 0f) transform.localScale = new Vector3(-0.5f, 1f, 1f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Slide State
        // ─────────────────────────────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isSliding)
                TryExitSlide(collision);
            else
                TryEnterSlide(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            // Handles the case where the slope curves into flat terrain within
            // a single continuous collider contact — exit would never fire from
            // OnCollisionEnter2D alone in that geometry.
            if (!_isSliding) return;
            TryExitSlide(collision);
        }

        private void TryEnterSlide(Collision2D collision)
        {
            if (collision.collider.sharedMaterial == null) return;
            if (collision.collider.sharedMaterial.name != _slopeMaterialName) return;

            foreach (ContactPoint2D contact in collision.contacts)
            {
                float angle = Vector2.Angle(contact.normal, Vector2.up);
                if (angle > _flatGroundAngleThreshold)
                {
                    EnterSlideState();
                    return;
                }
            }
        }

        private void TryExitSlide(Collision2D collision)
        {
            // Exit check is material-agnostic — the landing surface may or may
            // not share the SlipperySlope material; only the normal angle matters.
            foreach (ContactPoint2D contact in collision.contacts)
            {
                float angle = Vector2.Angle(contact.normal, Vector2.up);
                if (angle <= _flatGroundAngleThreshold)
                {
                    ExitSlideState();
                    return;
                }
            }
        }

        private void EnterSlideState()
        {
            if (_isSliding) return;
            _isSliding = true;
            _isDecelerating = false;
            GameEvents.RaiseSlideStateChanged(true);
        }

        private void ExitSlideState()
        {
            if (!_isSliding) return;
            _isSliding = false;
            _isDecelerating = true;
            GameEvents.RaiseSlideStateChanged(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Adaptation Bonus Queries
        // ─────────────────────────────────────────────────────────────────────

        private float GetJumpBonus() =>
            _adaptationManager != null ? _adaptationManager.GetJumpBonus() : 0f;

        private float GetMoveSpeedBonus() =>
            _adaptationManager != null ? _adaptationManager.GetMoveSpeedBonus() : 0f;

        // ─────────────────────────────────────────────────────────────────────
        // Public Accessors
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Whether Dr. Maria is currently in the slide state.</summary>
        public bool IsSliding => _isSliding;

        /// <summary>Whether Dr. Maria is currently on the ground.</summary>
        public bool IsGrounded => _isGrounded;

        // ─────────────────────────────────────────────────────────────────────
        // Debug
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (_groundCheck == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
        }
    }
}