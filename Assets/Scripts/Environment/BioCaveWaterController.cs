// BioCaveWaterController.cs
// Namespace: TerrasHeart.World
// Attach to: the water surface GameObject (same object as the MeshRenderer using BioCaveWater.mat)
//
// Subscribes to GameEvents.OnBiomeHealthChanged.
// When this biome's health changes, lerps water shader properties to reflect ecological state:
//   - Healthy (>60%)  : bright cyan (#00FFD1), warm gold rim (#FFB347), high glow
//   - Stressed (30-60%): desaturating, rim dims, glow fades
//   - Critical (<30%) : toxic amber (#C8860A) dominant, glow mostly gone, dark deep colour
//
// All colour lerps match EcologicalHealthVolume thresholds for visual consistency.
// This keeps the water in sync with the global biome health visual language —
// ecological consequence (Design Pillar 2) reads from every surface in the room.

using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.World
{
    [RequireComponent(typeof(Renderer))]
    public class BioCaveWaterController : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Inspector
        // ------------------------------------------------------------------

        [Header("Biome")]
        [Tooltip("Must match BiomeHealthConfigSO.BiomeID for this scene.")]
        [SerializeField] private string _biomeID = "BrineglowDescent";

        [Header("Healthy State (biome > 60%)")]
        [SerializeField] private Color _shallowColorHealthy  = new Color(0f, 1f, 0.82f, 1f);   // #00FFD1
        [SerializeField] private Color _deepColorHealthy     = new Color(0f, 0.23f, 0.23f, 1f); // #003A3A
        [SerializeField] private Color _rimColorHealthy      = new Color(1f, 0.70f, 0.28f, 1f); // #FFB347
        [SerializeField] private float _glowIntensityHealthy = 2.5f;
        [SerializeField] private float _rimIntensityHealthy  = 3.0f;

        [Header("Critical State (biome < 30%)")]
        [Tooltip("Toxic amber (#C8860A) replaces cyan — matches EcologicalHealthVolume.")]
        [SerializeField] private Color _shallowColorCritical  = new Color(0.78f, 0.53f, 0.04f, 1f); // #C8860A
        [SerializeField] private Color _deepColorCritical     = new Color(0.10f, 0.07f, 0.01f, 1f); // very dark
        [SerializeField] private Color _rimColorCritical      = new Color(0.78f, 0.53f, 0.04f, 1f); // same amber
        [SerializeField] private float _glowIntensityCritical = 0.3f;
        [SerializeField] private float _rimIntensityCritical  = 0.5f;

        [Header("Transition")]
        [Tooltip("How quickly the water responds to health changes (lower = slower lerp).")]
        [SerializeField] private float _lerpSpeed = 1.5f;

        // ------------------------------------------------------------------
        // Shader property IDs — cached at Awake to avoid per-frame string hashing
        // ------------------------------------------------------------------

        private static readonly int _ShallowColorID  = Shader.PropertyToID("_ShallowColor");
        private static readonly int _DeepColorID      = Shader.PropertyToID("_DeepColor");
        private static readonly int _RimColorID       = Shader.PropertyToID("_RimColor");
        private static readonly int _GlowIntensityID  = Shader.PropertyToID("_GlowIntensity");

        // ------------------------------------------------------------------
        // Private state
        // ------------------------------------------------------------------

        private Renderer        _renderer;
        private MaterialPropertyBlock _mpb;
        private float           _targetHealth;
        private float           _currentHealth;

        // Current lerped property values
        private Color  _currentShallow;
        private Color  _currentDeep;
        private Color  _currentRim;
        private float  _currentGlow;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb      = new MaterialPropertyBlock();

            // Initialise at whatever the BiomeHealthManager currently holds.
            // BiomeHealthManager starts biomes at their configured StartHealth,
            // so we read current health on first frame rather than assuming 100%.
            // For now prime at 65% (BrineglowDescent default) — will be corrected
            // immediately when BiomeHealthManager fires OnBiomeHealthChanged on Start.
            _targetHealth  = 65f;
            _currentHealth = 65f;
            ApplyProperties(NormaliseHealth(_currentHealth));
        }

        private void OnEnable()
        {
            GameEvents.OnBiomeHealthChanged += HandleBiomeHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnBiomeHealthChanged -= HandleBiomeHealthChanged;
        }

        private void Update()
        {
            // Smoothly lerp current health toward target for gradual visual transitions.
            if (Mathf.Approximately(_currentHealth, _targetHealth)) return;

            _currentHealth = Mathf.MoveTowards(_currentHealth, _targetHealth,
                                                _lerpSpeed * Time.deltaTime * 100f);
            ApplyProperties(NormaliseHealth(_currentHealth));
        }

        // ------------------------------------------------------------------
        // Event handler
        // ------------------------------------------------------------------

        private void HandleBiomeHealthChanged(string biomeID, float healthPercent)
        {
            if (biomeID != _biomeID) return;
            _targetHealth = healthPercent;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Normalises the 0-100 health float to a 0-1 t value for lerping:
        ///   t=1 → fully healthy (#00FFD1 glow)
        ///   t=0 → fully critical (toxic amber)
        /// Uses the same 30/60 thresholds as EcologicalHealthVolume.
        /// </summary>
        private static float NormaliseHealth(float health)
        {
            return Mathf.Clamp01((health - 30f) / 30f); // 30→0 maps to t=0, 60→100 maps to t=1
        }

        private void ApplyProperties(float t)
        {
            // Cache renderer if not already assigned (needed for Edit mode context menu calls)
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();
            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();

            // HDR colours: multiply base colour by intensity scalar for HDR output
            _currentShallow = Color.Lerp(_shallowColorCritical, _shallowColorHealthy, t)
                              * Mathf.Lerp(_glowIntensityCritical, _glowIntensityHealthy, t);

            _currentDeep = Color.Lerp(_deepColorCritical, _deepColorHealthy, t);

            _currentRim = Color.Lerp(_rimColorCritical, _rimColorHealthy, t)
                              * Mathf.Lerp(_rimIntensityCritical, _rimIntensityHealthy, t);

            _currentGlow = Mathf.Lerp(_glowIntensityCritical, _glowIntensityHealthy, t);

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_ShallowColorID, _currentShallow);
            _mpb.SetColor(_DeepColorID, _currentDeep);
            _mpb.SetColor(_RimColorID, _currentRim);
            _mpb.SetFloat(_GlowIntensityID, _currentGlow);
            _renderer.SetPropertyBlock(_mpb);
        }

        // ------------------------------------------------------------------
        // Editor helper — lets you preview the colour states in the Inspector
        // without entering Play mode (call from a custom editor or context menu).
        // ------------------------------------------------------------------
#if UNITY_EDITOR
        [ContextMenu("Preview Healthy State")]
        private void PreviewHealthy()
        {
            _currentHealth = 100f;
            Awake();
            ApplyProperties(NormaliseHealth(100f));
        }

        [ContextMenu("Preview Critical State")]
        private void PreviewCritical()
        {
            _currentHealth = 0f;
            ApplyProperties(NormaliseHealth(0f));
        }

        [ContextMenu("Preview Stressed State (50%)")]
        private void PreviewStressed()
        {
            _currentHealth = 50f;
            ApplyProperties(NormaliseHealth(50f));
        }
#endif
    }
}
