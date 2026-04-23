// WaterSubmersionController.cs
// Namespace: TerrasHeart.Environment
// Attach to: WaterSurface_Pool01 (same GameObject as WaterSpringController)
//
// Detects when DrMaria enters the water volume and applies increased
// Rigidbody2D linearDamping and reduced gravityScale to simulate water resistance.
//
// Submersion state is determined every FixedUpdate by comparing DrMaria's
// world Y position against the water surface Y — not by OnTriggerExit2D,
// which is unreliable when exiting through the top of a trigger volume.
//
// Original damping and gravity values are cached at Awake from the assigned
// _playerRigidbody reference — before any submersion can modify them.
//
// Ecological consequence (Design Pillar 2):
// Water resistance scales with biome health — healthy water is denser;
// critical degraded water is thin and barely slows the player.

using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.Environment
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class WaterSubmersionController : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Inspector
        // ------------------------------------------------------------------

        [Header("Submersion")]
        [Tooltip("Layers that receive water resistance. Include Player layer.")]
        [SerializeField] private LayerMask _affectedLayers;

        [Tooltip("DrMaria's Rigidbody2D — assign in Inspector. " +
                 "Original damping and gravity are read from this at startup.")]
        [SerializeField] private Rigidbody2D _playerRigidbody;

        [Tooltip("linearDamping applied while submerged at full biome health.")]
        [SerializeField] private float _waterDampingHealthy = 8f;

        [Tooltip("linearDamping at critical biome health. Degraded water barely slows the player.")]
        [SerializeField] private float _waterDampingCritical = 2f;

        [Tooltip("Gravity scale while submerged. 0.4 = floaty slow fall. 1.0 = no change.")]
        [SerializeField] private float _submergedGravityScale = 0.4f;

        [Tooltip("Y offset from this GameObject's position that defines the water surface. " +
                 "Auto-set from mesh bounds top edge in Start — override if needed.")]
        [SerializeField] private float _waterSurfaceLocalY = 0f;

        [Header("Ecological Health Response")]
        [Tooltip("Must match BiomeHealthConfigSO.BiomeID for this scene.")]
        [SerializeField] private string _biomeID = "BrineglowDescent";

        // ------------------------------------------------------------------
        // Private
        // ------------------------------------------------------------------

        private BoxCollider2D _volumeCollider;
        private float _activeDamping;

        // Original values cached once at Awake — before any submersion modifies them
        private float _originalDamping;
        private float _originalGravityScale;

        private Transform _trackedTransform;
        private bool _isSubmerged;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        private void Awake()
        {
            TryGetComponent(out _volumeCollider);
            if (_volumeCollider != null)
                _volumeCollider.isTrigger = true;

            // Cache original values now — before any water interaction
            if (_playerRigidbody != null)
            {
                _originalDamping = _playerRigidbody.linearDamping;
                _originalGravityScale = _playerRigidbody.gravityScale;
                _trackedTransform = _playerRigidbody.transform;
            }
            else
            {
                Debug.LogWarning("[WaterSubmersionController] _playerRigidbody not assigned in Inspector.");
            }

            UpdateDamping(NormaliseHealth(65f));
        }

        private void Start()
        {
            // Read the water surface Y from the mesh bounds top edge
            if (TryGetComponent(out MeshFilter mf) && mf.mesh != null)
                _waterSurfaceLocalY = mf.mesh.bounds.max.y;
        }

        private void OnEnable()
        {
            GameEvents.OnBiomeHealthChanged += HandleBiomeHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnBiomeHealthChanged -= HandleBiomeHealthChanged;

            if (_isSubmerged && _playerRigidbody != null)
                RestoreDamping();
        }

        private void FixedUpdate()
        {
            if (_playerRigidbody == null || _trackedTransform == null) return;

            float waterSurfaceWorldY = transform.TransformPoint(
                new Vector3(0f, _waterSurfaceLocalY, 0f)).y;

            bool shouldBeSubmerged = _trackedTransform.position.y < waterSurfaceWorldY - 0.1f;

            // Exit immediately when player has upward velocity near the surface
            if (_isSubmerged && _playerRigidbody.linearVelocity.y > 0.5f &&
                _trackedTransform.position.y > waterSurfaceWorldY - 0.5f)
                shouldBeSubmerged = false;

            if (shouldBeSubmerged && !_isSubmerged)
                ApplySubmersion();
            else if (!shouldBeSubmerged && _isSubmerged)
                RestoreDamping();
        }

        // ------------------------------------------------------------------
        // Trigger — no longer used for state, kept for safety only
        // ------------------------------------------------------------------

        private void OnTriggerEnter2D(Collider2D other) { }
        private void OnTriggerExit2D(Collider2D other) { }

        // ------------------------------------------------------------------
        // Submersion State
        // ------------------------------------------------------------------

        private void ApplySubmersion()
        {
            if (_playerRigidbody == null) return;

            _isSubmerged = true;
            _playerRigidbody.linearDamping = _activeDamping;
            _playerRigidbody.gravityScale = _submergedGravityScale;

            GameEvents.RaisePlayerSubmerged(true);
        }

        private void RestoreDamping()
        {
            if (_playerRigidbody == null) return;

            _isSubmerged = false;
            _playerRigidbody.linearDamping = _originalDamping;
            _playerRigidbody.gravityScale = _originalGravityScale;

            GameEvents.RaisePlayerSubmerged(false);
        }

        // ------------------------------------------------------------------
        // Ecological Health
        // ------------------------------------------------------------------

        private void HandleBiomeHealthChanged(string biomeID, float healthPercent)
        {
            if (biomeID != _biomeID) return;
            UpdateDamping(NormaliseHealth(healthPercent));

            if (_isSubmerged && _playerRigidbody != null)
                _playerRigidbody.linearDamping = _activeDamping;
        }

        private void UpdateDamping(float t)
        {
            _activeDamping = Mathf.Lerp(_waterDampingCritical, _waterDampingHealthy, t);
        }

        private static float NormaliseHealth(float health)
        {
            return Mathf.Clamp01((health - 30f) / 30f);
        }
    }
}