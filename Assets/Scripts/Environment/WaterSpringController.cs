// WaterSpringController.cs
// Namespace: TerrasHeart.Environment
// Attach to: WaterSurface_Pool01 (same GameObject as MeshFilter, MeshRenderer, BioCaveWaterController)
//
// Spring-based interactive wave system for BioCaveWater pools.
// Each top-edge vertex of the subdivided water mesh is treated as a spring.
// When DrMaria or other physics objects enter the water, nearby springs receive
// a velocity impulse. Spring math runs in FixedUpdate; energy propagates to
// neighbouring springs and dampens over time.
//
// The shader's sine-wave vertex displacement (Stage 7 of BioCaveWater.shadergraph)
// provides the ambient surface undulation. This system adds impulse-driven
// interactive ripples ON TOP of that ambient baseline — they are additive.
//
// Ecological consequence (Design Pillar 2):
// Spring responsiveness is driven by biome health via GameEvents.OnBiomeHealthChanged.
// Healthy water (>60%) = springy, reactive, glowing ripples.
// Critical water (<30%) = sluggish, heavily damped, barely reacts.
//
// Based on: Daniel Ilett's spring water tutorial (YouTube), adapted for
// Terra's Heart architecture — Unity 6, New Input System, GameEvents bus,
// TerrasHeart namespaces.

using System.Collections.Generic;
using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.Environment
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class WaterSpringController : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Inspector — Spring Physics
        // ------------------------------------------------------------------

        [Header("Spring Physics")]
        [Tooltip("How strongly springs pull back toward their rest position. Higher = stiffer water.")]
        [SerializeField] private float _springStiffness = 0.08f;

        [Tooltip("Velocity damping per FixedUpdate. Higher = water settles faster.")]
        [SerializeField] private float _damping = 0.12f;

        [Tooltip("How many propagation iterations per FixedUpdate. Higher = faster wave spread.")]
        [SerializeField] private int _propagationIterations = 8;

        [Tooltip("How much energy is transferred to neighbouring springs per iteration.")]
        [SerializeField] private float _spread = 0.12f;

        [Tooltip("Speed multiplier applied to all spring velocity calculations.")]
        [SerializeField] private float _speedMultiplier = 3.0f;

        [Header("Splash Force")]
        [Tooltip("Max velocity clamp — prevents fast-moving objects exploding the springs.")]
        [SerializeField] private float _maxSplashVelocity = 0.15f;

        // ------------------------------------------------------------------
        // Inspector — Ecological Health Response
        // ------------------------------------------------------------------

        [Header("Ecological Health Response")]
        [Tooltip("Must match BiomeHealthConfigSO.BiomeID for this scene.")]
        [SerializeField] private string _biomeID = "BrineglowDescent";

        [Tooltip("Spring stiffness multiplier at full health (100%). Water is reactive and springy.")]
        [SerializeField] private float _stiffnessMultiplierHealthy = 1.0f;

        [Tooltip("Spring stiffness multiplier at critical health (0%). Water is sluggish and barely reacts.")]
        [SerializeField] private float _stiffnessMultiplierCritical = 0.15f;

        [Tooltip("Damping multiplier at full health. Low damping = longer-lasting ripples.")]
        [SerializeField] private float _dampingMultiplierHealthy = 1.0f;

        [Tooltip("Damping multiplier at critical health. High damping = ripples die immediately.")]
        [SerializeField] private float _dampingMultiplierCritical = 4.0f;

        [Tooltip("Force multiplier at full health.")]
        [SerializeField] private float _forceMultiplierHealthy = 1.0f;

        [Tooltip("Force multiplier at critical health. Degraded water barely reacts to disturbance.")]
        [SerializeField] private float _forceMultiplierCritical = 0.1f;

        // ------------------------------------------------------------------
        // Inspector — References
        // ------------------------------------------------------------------

        [Header("References")]
        [Tooltip("The EdgeCollider2D used as the water trigger. Set automatically by WaterTriggerHandler.")]
        [SerializeField] private EdgeCollider2D _edgeCollider;

        // ------------------------------------------------------------------
        // Private — Water Point Data
        // ------------------------------------------------------------------

        /// <summary>
        /// Represents a single top-edge vertex treated as a spring.
        /// localX:  fixed local-space X — stored at init, used directly for EdgeCollider updates.
        /// worldX:  fixed world-space X — used for splash distance checks.
        /// position: current Y world position — changes as the spring oscillates.
        /// targetHeight: rest Y world position — the spring always tries to return here.
        /// </summary>
        private class WaterPoint
        {
            public float localX;        // Fixed local X — stored once, used for EdgeCollider
            public float worldX;        // Fixed world X — used for splash distance checks
            public float velocity;
            public float position;      // Current Y world position
            public float targetHeight;  // Rest Y world position
        }

        private List<WaterPoint> _waterPoints = new List<WaterPoint>();
        private int[] _topVertexIndices;

        // ------------------------------------------------------------------
        // Private — Mesh
        // ------------------------------------------------------------------

        private Mesh _mesh;
        private Vector3[] _vertices;

        // ------------------------------------------------------------------
        // Private — Health State
        // ------------------------------------------------------------------

        private float _currentHealthT = 1f;

        private float _activeStiffness;
        private float _activeDamping;
        private float _activeForceMultiplier;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        private void Awake()
        {
            _mesh = GetComponent<MeshFilter>().mesh;
            _vertices = _mesh.vertices;
            UpdateHealthMultipliers(NormaliseHealth(65f));
        }

        private void Start()
        {
            InitialiseWaterPoints();
            AutoSizeEdgeCollider();
        }

        private void OnEnable()
        {
            GameEvents.OnBiomeHealthChanged += HandleBiomeHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnBiomeHealthChanged -= HandleBiomeHealthChanged;
        }

        private void FixedUpdate()
        {
            if (_waterPoints == null || _waterPoints.Count == 0) return;

            UpdateSprings();
            PropagateWaves();
            UpdateMeshVertices();
            UpdateEdgeCollider();
        }

        // ------------------------------------------------------------------
        // Initialisation
        // ------------------------------------------------------------------

        private void InitialiseWaterPoints()
        {
            _waterPoints.Clear();

            if (_mesh == null || _mesh.vertexCount == 0)
            {
                Debug.LogWarning("[WaterSpringController] No mesh found — call WaterMeshGenerator first.");
                return;
            }

            _vertices = _mesh.vertices;

            // Find maximum Y in object space — top-edge vertices
            float maxY = float.MinValue;
            foreach (var v in _vertices)
                if (v.y > maxY) maxY = v.y;

            var topIndices = new List<int>();
            float tolerance = 0.001f;
            for (int i = 0; i < _vertices.Length; i++)
            {
                if (Mathf.Abs(_vertices[i].y - maxY) < tolerance)
                    topIndices.Add(i);
            }

            // Sort left to right by X
            topIndices.Sort((a, b) => _vertices[a].x.CompareTo(_vertices[b].x));
            _topVertexIndices = topIndices.ToArray();

            foreach (int idx in _topVertexIndices)
            {
                Vector3 localPos = _vertices[idx];
                Vector3 worldPos = transform.TransformPoint(localPos);

                _waterPoints.Add(new WaterPoint
                {
                    localX = localPos.x,  // Store local X directly — no conversion needed later
                    worldX = worldPos.x,  // Store world X for splash distance checks
                    velocity = 0f,
                    position = worldPos.y,
                    targetHeight = worldPos.y
                });
            }

            Debug.Log($"[WaterSpringController] Initialised {_waterPoints.Count} spring points. " +
                      $"X range: {_waterPoints[0].worldX:F2} to {_waterPoints[_waterPoints.Count - 1].worldX:F2}");
        }

        public void AutoSizeEdgeCollider()
        {
            if (_edgeCollider == null)
            {
                if (!TryGetComponent(out _edgeCollider))
                {
                    Debug.LogWarning("[WaterSpringController] No EdgeCollider2D found.");
                    return;
                }
            }

            if (_topVertexIndices == null || _topVertexIndices.Length < 2) return;

            Vector3 leftLocal = _vertices[_topVertexIndices[0]];
            Vector3 rightLocal = _vertices[_topVertexIndices[_topVertexIndices.Length - 1]];

            Vector2 offset = _edgeCollider.offset;
            var points = new Vector2[]
            {
                new Vector2(leftLocal.x  - offset.x, leftLocal.y - offset.y),
                new Vector2(rightLocal.x - offset.x, rightLocal.y - offset.y)
            };

            _edgeCollider.SetPoints(new List<Vector2>(points));
        }

        // ------------------------------------------------------------------
        // Spring Physics
        // ------------------------------------------------------------------

        private void UpdateSprings()
        {
            int count = _waterPoints.Count;

            for (int i = 1; i < count - 1; i++)
            {
                WaterPoint p = _waterPoints[i];

                float displacement = p.position - p.targetHeight;
                float acceleration = -_activeStiffness * displacement - _activeDamping * p.velocity;

                p.velocity += acceleration * _speedMultiplier * Time.fixedDeltaTime;
                p.position += p.velocity * _speedMultiplier * Time.fixedDeltaTime;
            }
        }

        private void PropagateWaves()
        {
            int count = _waterPoints.Count;

            for (int iter = 0; iter < _propagationIterations; iter++)
            {
                for (int i = 1; i < count - 1; i++)
                {
                    float leftDelta = _spread * (_waterPoints[i].position - _waterPoints[i - 1].position);
                    float rightDelta = _spread * (_waterPoints[i].position - _waterPoints[i + 1].position);

                    _waterPoints[i - 1].velocity += leftDelta * _speedMultiplier * Time.fixedDeltaTime;
                    _waterPoints[i + 1].velocity += rightDelta * _speedMultiplier * Time.fixedDeltaTime;
                }
            }
        }

        private void UpdateMeshVertices()
        {
            _vertices = _mesh.vertices;

            for (int i = 0; i < _topVertexIndices.Length; i++)
            {
                int meshIdx = _topVertexIndices[i];
                float worldY = _waterPoints[i].position;
                float localY = transform.InverseTransformPoint(new Vector3(0f, worldY, 0f)).y;

                _vertices[meshIdx] = new Vector3(
                    _vertices[meshIdx].x,
                    localY,
                    _vertices[meshIdx].z);
            }

            _mesh.vertices = _vertices;
        }

        /// <summary>
        /// Updates the EdgeCollider2D points every FixedUpdate to follow the animated spring positions.
        /// Uses stored localX (set at init) and converts only the Y back from world to local space.
        /// The green collider line now tracks the water surface silhouette in real time.
        /// </summary>
        private void UpdateEdgeCollider()
        {
            if (_edgeCollider == null) return;

            var points = new List<Vector2>(_waterPoints.Count);
            Vector2 offset = _edgeCollider.offset;

            for (int i = 0; i < _waterPoints.Count; i++)
            {
                // localX is stored directly — no conversion needed
                // Convert world Y back to local Y for the collider
                float localY = transform.InverseTransformPoint(
                    new Vector3(_waterPoints[i].worldX, _waterPoints[i].position, 0f)).y;

                points.Add(new Vector2(
                    _waterPoints[i].localX - offset.x,
                    localY - offset.y));
            }

            _edgeCollider.SetPoints(points);
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Applies a velocity impulse to all springs within horizontal radius of the splash center.
        /// Distance is checked on the X axis — splash center X vs each spring's fixed world X.
        /// </summary>
        public void Splash(Vector2 splashCenter, float radius, float force)
        {
            if (_waterPoints == null || _waterPoints.Count == 0)
            {
                Debug.LogWarning("[WaterSpringController] Splash called but water points are empty.");
                return;
            }

            float scaledForce = Mathf.Clamp(force * _activeForceMultiplier, -_maxSplashVelocity, _maxSplashVelocity);
            int hitsCount = 0;

            for (int i = 1; i < _waterPoints.Count - 1; i++)
            {
                float distance = Mathf.Abs(_waterPoints[i].worldX - splashCenter.x);

                if (distance <= radius)
                {
                    float attenuation = 1f - (distance / radius);
                    _waterPoints[i].velocity += scaledForce * attenuation;
                    hitsCount++;
                }
            }

            Debug.Log($"[WaterSpringController] Splash — center: {splashCenter}, radius: {radius}, " +
                      $"scaledForce: {scaledForce}, springs hit: {hitsCount}");
        }

        // ------------------------------------------------------------------
        // Ecological Health
        // ------------------------------------------------------------------

        private void HandleBiomeHealthChanged(string biomeID, float healthPercent)
        {
            if (biomeID != _biomeID) return;
            UpdateHealthMultipliers(NormaliseHealth(healthPercent));
        }

        private void UpdateHealthMultipliers(float t)
        {
            _currentHealthT = t;
            _activeStiffness = _springStiffness * Mathf.Lerp(_stiffnessMultiplierCritical, _stiffnessMultiplierHealthy, t);
            _activeDamping = _damping * Mathf.Lerp(_dampingMultiplierCritical, _dampingMultiplierHealthy, t);
            _activeForceMultiplier = Mathf.Lerp(_forceMultiplierCritical, _forceMultiplierHealthy, t);
        }

        private static float NormaliseHealth(float health)
        {
            return Mathf.Clamp01((health - 30f) / 30f);
        }

        // ------------------------------------------------------------------
        // Editor Helpers
        // ------------------------------------------------------------------

#if UNITY_EDITOR
        [ContextMenu("Reinitialise Water Points")]
        private void EditorReinitialise()
        {
            _mesh     = GetComponent<MeshFilter>().sharedMesh;
            _vertices = _mesh.vertices;
            InitialiseWaterPoints();
            AutoSizeEdgeCollider();
            Debug.Log("[WaterSpringController] Reinitialised from editor.");
        }
#endif
    }
}