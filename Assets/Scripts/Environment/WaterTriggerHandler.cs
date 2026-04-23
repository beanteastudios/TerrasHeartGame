// WaterTriggerHandler.cs
// Namespace: TerrasHeart.Environment
// Attach to: WaterSurface_Pool01 (same GameObject as WaterSpringController)
//
// Detects when DrMaria or other physics objects enter or exit the water trigger.
// On enter: calls WaterSpringController.Splash() with force based on the
// entering object's Rigidbody2D velocity.
//
// Layer setup required:
// - The water GameObject must have a Layer assigned (recommend: "Water")
// - The EdgeCollider2D on this GameObject must have Is Trigger = true
// - _splashLayerMask in the Inspector must include the layers that should
//   trigger splashes (e.g. "Player", "Debris")
//
// Design note: Entry from above pushes springs DOWN (positive force).
// Entry from below pushes springs UP (negative force — player surfacing).
// This matches the ecological reading — Dr. Maria disturbing the water from
// above is a surface entry; from below is an emergence.

using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.Environment
{
    [RequireComponent(typeof(EdgeCollider2D))]
    [RequireComponent(typeof(WaterSpringController))]
    public class WaterTriggerHandler : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Inspector
        // ------------------------------------------------------------------

        [Header("Splash Detection")]
        [Tooltip("Layers that trigger a splash when entering the water. Include Player layer.")]
        [SerializeField] private LayerMask _splashLayerMask;

        [Tooltip("Multiplier applied to the entering Rigidbody2D velocity to calculate splash force. Tune to taste.")]
        [SerializeField] private float _forceMultiplier = 0.1f;

        [Tooltip("Radius of the splash area in world units. Based on collider bounds × this multiplier.")]
        [SerializeField] private float _splashRadiusMultiplier = 1.5f;

        [Tooltip("Minimum vertical velocity required to register a splash. Prevents tiny touches triggering waves.")]
        [SerializeField] private float _minSplashVelocity = 0.5f;

        [Header("Splash Particles")]
        [Tooltip("Optional particle system to spawn on water entry. Leave empty if none.")]
        [SerializeField] private ParticleSystem _splashParticles;

        [Tooltip("Spawn particles only when entering from above (downward velocity).")]
        [SerializeField] private bool _particlesOnEntryOnly = true;

        // ------------------------------------------------------------------
        // Private
        // ------------------------------------------------------------------

        private WaterSpringController _springController;
        private EdgeCollider2D _edgeCollider;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        private void Awake()
        {
            _springController = GetComponent<WaterSpringController>();
            _edgeCollider = GetComponent<EdgeCollider2D>();
            _edgeCollider.isTrigger = true;
        }

        // ------------------------------------------------------------------
        // Trigger Detection
        // ------------------------------------------------------------------

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check layer mask
            if ((_splashLayerMask.value & (1 << other.gameObject.layer)) == 0) return;

            // Try to get Rigidbody2D — check parent in case collider is on a child object
            Rigidbody2D rb = other.GetComponentInParent<Rigidbody2D>();
            if (rb == null) return;

            float verticalVelocity = rb.linearVelocity.y;

            // Ignore very slow entries — prevents micro-touches triggering waves
            if (Mathf.Abs(verticalVelocity) < _minSplashVelocity) return;

            // Calculate splash parameters
            Vector2 splashCenter = new Vector2(other.bounds.center.x, other.bounds.center.y);
            float radius = other.bounds.extents.x * _splashRadiusMultiplier;

            // Force direction: entering from above = negative Y velocity = push springs down (positive force)
            // Entering from below = positive Y velocity = push springs up (negative force)
            float force = -verticalVelocity * _forceMultiplier;

            Debug.Log($"[WaterTriggerHandler] Calling Splash — center: {splashCenter}, radius: {radius}, force: {force}");

            // Apply splash to spring system
            _springController.Splash(splashCenter, radius, force);

            // Spawn particles if assigned
            if (_splashParticles != null)
            {
                bool enteringFromAbove = verticalVelocity < 0f;
                if (!_particlesOnEntryOnly || enteringFromAbove)
                {
                    _splashParticles.transform.position = new Vector3(
                        splashCenter.x,
                        splashCenter.y,
                        _splashParticles.transform.position.z);
                    _splashParticles.Play();
                }
            }

            // Raise GameEvent for future systems (e.g. sound, scan triggers, bioluminescent flash)
            GameEvents.RaiseWaterSplash(splashCenter, radius, force);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // On exit, apply a small upward force as the object surfaces
            if ((_splashLayerMask.value & (1 << other.gameObject.layer)) == 0) return;

            Rigidbody2D rb = other.GetComponentInParent<Rigidbody2D>();
            if (rb == null) return;

            float exitVelocity = rb.linearVelocity.y;
            if (Mathf.Abs(exitVelocity) < _minSplashVelocity) return;

            Vector2 exitCenter = new Vector2(other.bounds.center.x, other.bounds.center.y);
            float radius = other.bounds.extents.x * _splashRadiusMultiplier;
            float force = -exitVelocity * _forceMultiplier * 0.5f; // Exit force is half of entry

            _springController.Splash(exitCenter, radius, force);
        }
    }
}