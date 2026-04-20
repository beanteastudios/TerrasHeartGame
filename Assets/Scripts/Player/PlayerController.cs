// ─────────────────────────────────────────────────────────────────────────────
// PlayerController.cs
// Path: Assets/Scripts/Player/PlayerController.cs
// Terra's Heart — Dr. Maria's movement controller.
//
// Updated:
//   - Added slow walk (Left Shift held) — reduces move speed below creature
//     detection threshold. Tune _slowWalkSpeed to stay below
//     CaveLuminothAI._slowSpeedThreshold (currently 1.5).
//   - Added throw input (T key) — fires GameEvents.RaiseThrowInput().
//   - Phase B Step 4: Added palette input (1, 2 keys) — fires
//     GameEvents.RaisePaletteInput(index) for Glow-Mantle call-and-response.
//     Always fires on key press — GlowMantleAI filters by state.
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

        [Header("Adaptations")]
        [Tooltip("Assign the AdaptationManager component on DrMaria.")]
        [SerializeField] private AdaptationManager _adaptationManager;

        // ─── Private State ────────────────────────────────────────────────────

        private Rigidbody2D _rb;
        private bool _isGrounded;
        private Vector2 _moveInput;
        private bool _isSlowWalking;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
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
            float speed = _isSlowWalking
                ? _slowWalkSpeed
                : _moveSpeed + GetMoveSpeedBonus();

            _rb.linearVelocity = new Vector2(_moveInput.x * speed, _rb.linearVelocity.y);
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
            _isGrounded = Physics2D.OverlapCircle(
                _groundCheck.position, _groundCheckRadius, _groundLayer);
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
        // Adaptation Bonus Queries
        // ─────────────────────────────────────────────────────────────────────

        private float GetJumpBonus() =>
            _adaptationManager != null ? _adaptationManager.GetJumpBonus() : 0f;

        private float GetMoveSpeedBonus() =>
            _adaptationManager != null ? _adaptationManager.GetMoveSpeedBonus() : 0f;
    }
}