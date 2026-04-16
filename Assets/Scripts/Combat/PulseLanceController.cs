// ─────────────────────────────────────────────────────────────────────────────
// PulseLanceController.cs
// Path: Assets/Scripts/Combat/PulseLanceController.cs
// Terra's Heart — Dr. Maria's Pulse Lance. Step 5 minimum implementation.
//
// Attach to: DrMaria
//
// PURPOSE IN STEP 5:
//   The Pulse Lance exists solely to subdue the Tarn Creeper in Room 6.
//   It calls TarnCreeperAI.ApplySubdueDamage() on hit.
//   It does NOT deal lethal damage. It does NOT interact with any creature
//   other than TarnCreeperAI in Step 5.
//
// IMPLEMENTATION:
//   Raycast forward (facing direction) on fire.
//   If the hit object has a TarnCreeperAI component, apply subdue damage.
//   Simple raycast — no projectile visual in Step 5 (graybox).
//
// INPUT:
//   Left mouse button — Mouse.current.leftButton (New Input System).
//   Cooldown between shots: _fireCooldown seconds.
//
// ECOLOGICAL NOTE (for future combat tools):
//   The Pulse Lance in Step 5 cannot kill, so no ecological penalty fires here.
//   When additional tools are added in production, they must check creature
//   health state and fire GameEvents.OnBiomeHealthChanged with a penalty
//   if a living (non-corrupted) creature is killed.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;
using TerrasHeart.Scanner;
using TerrasHeart.Creatures;

namespace TerrasHeart.Combat
{
    public class PulseLanceController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Range of the Pulse Lance raycast in world units.")]
        [SerializeField] private float _range = 8f;

        [Tooltip("Subdue damage applied to TarnCreeperAI per hit.")]
        [SerializeField] private float _subduePerHit = 10f;

        [Tooltip("Minimum seconds between shots.")]
        [SerializeField] private float _fireCooldown = 0.4f;

        [Tooltip("Layer mask for the lance raycast. Include Scannable layer so " +
                 "corrupted creatures are detected. Exclude Player layer.")]
        [SerializeField] private LayerMask _hitLayerMask;

        [Header("Origin")]
        [Tooltip("Fire origin transform. Defaults to DrMaria pivot if left empty. " +
                 "Can reuse the ScanOrigin child at Y:0.5.")]
        [SerializeField] private Transform _fireOrigin;

        // ─── Runtime ──────────────────────────────────────────────────────────

        private float _cooldownTimer;
        private bool  _facingRight = true;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_fireOrigin == null)
                _fireOrigin = transform;
        }

        private void Update()
        {
            _cooldownTimer -= Time.deltaTime;

            TrackFacingDirection();
            HandleFireInput();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Input
        // ─────────────────────────────────────────────────────────────────────

        private void TrackFacingDirection()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                _facingRight = true;
            else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                _facingRight = false;
        }

        private void HandleFireInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame && _cooldownTimer <= 0f)
            {
                Fire();
                _cooldownTimer = _fireCooldown;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Fire
        // ─────────────────────────────────────────────────────────────────────

        private void Fire()
        {
            Vector2 origin    = _fireOrigin.position;
            Vector2 direction = _facingRight ? Vector2.right : Vector2.left;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, _range, _hitLayerMask);

            Debug.DrawRay(origin, direction * _range, Color.yellow, 0.2f);

            if (hit.collider == null)
            {
                Debug.Log("[PulseLance] Fired — no target.");
                return;
            }

            // TarnCreeperAI — subdue damage only
            if (hit.collider.TryGetComponent(out TarnCreeperAI creeper))
            {
                creeper.ApplySubdueDamage(_subduePerHit);
                Debug.Log($"[PulseLance] Hit TarnCreeper — subdue damage {_subduePerHit}.");
                return;
            }

            // Any other IScannable hit — log only, no damage in Step 5
            if (hit.collider.TryGetComponent(out IScannable _))
            {
                Debug.Log($"[PulseLance] Hit {hit.collider.name} — " +
                          "non-corrupted targets are not affected by Pulse Lance in Step 5.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (_fireOrigin == null) return;

            Gizmos.color = new Color(1f, 0.8f, 0f, 0.6f);
            Gizmos.DrawRay(_fireOrigin.position,
                           (_facingRight ? Vector2.right : Vector2.left) * _range);
        }
    }
}
