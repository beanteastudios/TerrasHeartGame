// ─────────────────────────────────────────────────────────────────────────────
// PlayerController.cs
// Path: Assets/Scripts/Player/PlayerController.cs
// Terra's Heart — Dr. Maria's movement controller.
//
// Updated from original:
//   - Added namespace TerrasHeart.Player
//   - Converted public fields to [SerializeField] private (project standard)
//   - Cached Rigidbody2D in Awake (not Start)
//   - Added AdaptationManager reference for jump and move speed bonuses
//   - Jump force: base + AdaptationManager.GetJumpBonus()
//   - Move speed: base + AdaptationManager.GetMoveSpeedBonus()
//
// ⚠ AFTER REPLACING THIS SCRIPT re-assign in Inspector:
//   - Ground Check  → drag GroundCheck child transform
//   - Ground Layer  → select Ground layer
//   - Adaptation Manager → drag AdaptationManager component (add it to DrMaria first)
//
// Input: Keyboard.current polling (Option A — matches ScannerController).
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;
using TerrasHeart.Adaptations;

namespace TerrasHeart.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _jumpForce = 10f;

        [Header("Ground Check")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundCheckRadius = 0.1f;
        [SerializeField] private LayerMask _groundLayer;

        [Header("Adaptations")]
        [Tooltip("Assign the AdaptationManager component on DrMaria. " +
                 "If left empty, adaptation bonuses are ignored (base stats only).")]
        [SerializeField] private AdaptationManager _adaptationManager;

        // ─── Private State ────────────────────────────────────────────────────

        private Rigidbody2D _rb;
        private bool        _isGrounded;
        private Vector2     _moveInput;

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
            FlipSprite();
        }

        private void FixedUpdate()
        {
            float speed = _moveSpeed + GetMoveSpeedBonus();
            _rb.linearVelocity = new Vector2(_moveInput.x * speed, _rb.linearVelocity.y);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Input
        // ─────────────────────────────────────────────────────────────────────

        private void HandleMovementInput(Keyboard kb)
        {
            float horizontal = 0f;
            if (kb.leftArrowKey.isPressed  || kb.aKey.isPressed) horizontal = -1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) horizontal =  1f;
            _moveInput = new Vector2(horizontal, 0f);
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

        // ─────────────────────────────────────────────────────────────────────
        // Ground Check
        // ─────────────────────────────────────────────────────────────────────

        private void CheckGround()
        {
            _isGrounded = Physics2D.OverlapCircle(
                _groundCheck.position,
                _groundCheckRadius,
                _groundLayer
            );
        }

        // ─────────────────────────────────────────────────────────────────────
        // Sprite Flip
        // ─────────────────────────────────────────────────────────────────────

        private void FlipSprite()
        {
            if (_moveInput.x > 0f) transform.localScale = new Vector3( 0.5f, 1f, 1f);
            if (_moveInput.x < 0f) transform.localScale = new Vector3(-0.5f, 1f, 1f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Adaptation Bonus Queries
        // ─────────────────────────────────────────────────────────────────────

        private float GetJumpBonus()
        {
            return _adaptationManager != null ? _adaptationManager.GetJumpBonus() : 0f;
        }

        private float GetMoveSpeedBonus()
        {
            return _adaptationManager != null ? _adaptationManager.GetMoveSpeedBonus() : 0f;
        }
    }
}
