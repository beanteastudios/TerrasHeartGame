// ─────────────────────────────────────────────────────────────────────────────
// ThrowController.cs
// Path: Assets/Scripts/Combat/ThrowController.cs
// Terra's Heart — Handles Dr. Maria's food throw.
//
// Attach to: DrMaria (alongside ScannerController, CraftingMaterialInventory)
//
// Flow:
//   T key → PlayerController fires GameEvents.OnThrowInput
//   ThrowController.HandleThrowInput:
//     1. Check CraftingMaterialInventory has enough food material
//     2. Consume the material
//     3. Calculate landing position (horizontal throw, ground raycast)
//     4. Instantiate FoodMarker prefab at landing point
//     5. Schedule Destroy after config lifetime
//   FoodMarker.Start() raises GameEvents.OnFoodPlaced(position)
//   Creature AI scripts subscribe to OnFoodPlaced and react.
//
// Phase B Step 2.
// Facing direction tracked via A/D input — mirrors ScannerController pattern.
// ThrowController does not hold a reference to any creature AI.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;
using TerrasHeart.Events;
using TerrasHeart.Environment;

namespace TerrasHeart.Combat
{
    [RequireComponent(typeof(CraftingMaterialInventory))]
    public class ThrowController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Assign ThrowConfig.asset from Assets/Data/ScriptableObjects/Combat/")]
        [SerializeField] private ThrowConfigSO _config;

        [Header("References")]
        [Tooltip("Throw origin transform. Assign ScanOrigin — same chest-height point " +
                 "used by the scanner, so food throws originate from the same position.")]
        [SerializeField] private Transform _throwOrigin;

        [Tooltip("Assign the FoodMarker prefab from Assets/Prefabs/Environment/")]
        [SerializeField] private GameObject _foodMarkerPrefab;

        // ─── Runtime State ────────────────────────────────────────────────────

        private CraftingMaterialInventory _materialInventory;
        private bool _facingRight = true;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _materialInventory = GetComponent<CraftingMaterialInventory>();

            if (_throwOrigin == null)
                _throwOrigin = transform;
        }

        private void OnEnable()
        {
            GameEvents.OnThrowInput += HandleThrowInput;
        }

        private void OnDisable()
        {
            GameEvents.OnThrowInput -= HandleThrowInput;
        }

        private void Update()
        {
            TrackFacingDirection();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Input Tracking
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

        // ─────────────────────────────────────────────────────────────────────
        // Throw Logic
        // ─────────────────────────────────────────────────────────────────────

        private void HandleThrowInput()
        {
            if (_config == null)
            {
                Debug.LogWarning("[ThrowController] ThrowConfigSO not assigned.");
                return;
            }

            if (_foodMarkerPrefab == null)
            {
                Debug.LogWarning("[ThrowController] FoodMarker prefab not assigned.");
                return;
            }

            if (!_materialInventory.Consume(_config.FoodMaterialType, _config.FoodCostPerThrow))
            {
                Debug.Log($"[ThrowController] Cannot throw — insufficient " +
                          $"{_config.FoodMaterialType} (need {_config.FoodCostPerThrow}).");
                return;
            }

            Vector2 landingPos = GetLandingPosition();
            GameObject marker = Instantiate(_foodMarkerPrefab, landingPos, Quaternion.identity);
            Destroy(marker, _config.MarkerLifetime);

            Debug.Log($"[ThrowController] Food thrown → {landingPos} | " +
                      $"Marker lifetime: {_config.MarkerLifetime}s");
        }

        private Vector2 GetLandingPosition()
        {
            float direction = _facingRight ? 1f : -1f;
            float landingX = _throwOrigin.position.x + direction * _config.ThrowRange;

            // Raycast downward from above the landing column to snap to the ground.
            // Starts 2 units above the throw origin Y to handle slopes and ledges.
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(landingX, _throwOrigin.position.y + 2f),
                Vector2.down,
                12f);

            float landingY = hit.collider != null
                ? hit.point.y
                : _throwOrigin.position.y;

            return new Vector2(landingX, landingY);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_throwOrigin == null) return;
            float range = _config != null ? _config.ThrowRange : 4f;
            float dir   = _facingRight ? 1f : -1f;

            Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
            Gizmos.DrawWireSphere(
                new Vector3(_throwOrigin.position.x + dir * range, _throwOrigin.position.y, 0f),
                0.3f);
            Gizmos.DrawLine(_throwOrigin.position,
                new Vector3(_throwOrigin.position.x + dir * range, _throwOrigin.position.y, 0f));
        }
#endif
    }
}