// ─────────────────────────────────────────────────────────────────────────────
// EcologicalHealthVolume.cs
// Path: Assets/Scripts/Environment/EcologicalHealthVolume.cs
// Terra's Heart — Translates biome health (0–100) into URP Volume visual state.
//
// Attach to: the Global Volume GameObject in the scene.
//
// Listens to GameEvents.OnBiomeHealthChanged and smoothly lerps URP
// ColorAdjustments parameters to reflect ecological health.
//
// Visual language (matches game bible):
//   Healthy (100): full saturation, white colour filter
//   Degraded (50):  moderate desaturation, slight amber tint
//   Critical (0):   heavy desaturation, Toxic Amber (#C8860A) dominant
//
// SETUP REQUIREMENTS:
//   1. The Volume component on this GameObject must have a Volume Profile assigned
//   2. The Volume Profile must have a ColorAdjustments override added
//   3. Both Saturation and Color Filter checkboxes must be TICKED in the override
//      (overrideState must be true — unticked parameters are ignored by URP)
//   4. This must be a scene-placed Volume, NOT the project default volume
//      (Unity caches default volumes — runtime changes have no effect on them)
//
// API confirmed against Unity 6000.4 URP:
//   Volume.profile.TryGet<ColorAdjustments>(out ca)  ✓
//   ca.saturation.value  (ClampedFloatParameter, -100 to 100)  ✓
//   ca.colorFilter.value (ColorParameter)  ✓
//   ca.saturation.overrideState = true  ✓
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TerrasHeart.Events;

namespace TerrasHeart.Environment
{
    [RequireComponent(typeof(Volume))]
    public class EcologicalHealthVolume : MonoBehaviour
    {
        [Header("Biome Filter")]
        [Tooltip("Only react to health events for this biome ID. " +
                 "Must match BiomeHealthConfigSO.BiomeID for this scene. " +
                 "Example: 'Prologue', 'ShallowCaves'")]
        [SerializeField] private string _biomeID = "Prologue";

        [Header("Visual Range — Healthy State (100%)")]
        [Tooltip("Saturation at full health. 0 = no change from source art. " +
                 "Positive values boost saturation — useful if your art is slightly muted.")]
        [Range(-100f, 100f)]
        [SerializeField] private float _healthySaturation = 0f;

        [Tooltip("Colour filter tint at full health. White = no tint.")]
        [SerializeField] private Color _healthyColorFilter = Color.white;

        [Header("Visual Range — Critical State (0%)")]
        [Tooltip("Saturation at 0% health. -70 gives strong desaturation without full greyscale. " +
                 "Full greyscale (-100) may be too severe for the player to read gameplay clearly.")]
        [Range(-100f, 0f)]
        [SerializeField] private float _criticalSaturation = -70f;

        [Tooltip("Colour filter tint at 0% health. " +
                 "Toxic Amber (#C8860A) is the confirmed degraded zone accent colour.")]
        [SerializeField] private Color _criticalColorFilter = new Color(0.784f, 0.525f, 0.039f); // #C8860A

        [Header("Transition")]
        [Tooltip("Speed at which the visuals lerp to the target state. " +
                 "Lower = slower, more cinematic. Higher = more responsive. Default: 2.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _lerpSpeed = 2f;

        // ─── Runtime State ────────────────────────────────────────────────────

        private Volume             _volume;
        private ColorAdjustments   _colorAdjustments;
        private float              _targetHealth = 100f;
        private float              _currentDisplayHealth = 100f;
        private bool               _volumeReady = false;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _volume = GetComponent<Volume>();
            InitialiseColorAdjustments();
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
            if (!_volumeReady) return;

            // Smoothly lerp display health toward the target value
            _currentDisplayHealth = Mathf.MoveTowards(
                _currentDisplayHealth,
                _targetHealth,
                _lerpSpeed * 100f * Time.deltaTime  // lerpSpeed units per second as % of full range
            );

            ApplyToVolume(_currentDisplayHealth);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Event Handler
        // ─────────────────────────────────────────────────────────────────────

        private void HandleBiomeHealthChanged(string biomeID, float newHealth)
        {
            if (biomeID != _biomeID) return;

            _targetHealth = newHealth;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Volume Control
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises ColorAdjustments from the Volume Profile.
        /// Must be called in Awake after _volume is assigned.
        /// Sets overrideState on both parameters so URP uses our runtime values.
        /// </summary>
        private void InitialiseColorAdjustments()
        {
            if (_volume == null)
            {
                Debug.LogWarning("[EcologicalHealthVolume] No Volume component found.");
                return;
            }

            // volume.profile creates a runtime instance — does not modify the disk asset.
            // Never use volume.sharedProfile here (that would modify the SO on disk).
            if (!_volume.profile.TryGet<ColorAdjustments>(out _colorAdjustments))
            {
                Debug.LogWarning("[EcologicalHealthVolume] ColorAdjustments override not found " +
                                 "in the Volume Profile. " +
                                 "Add it via: Volume Inspector → Add Override → Post-processing → " +
                                 "Color Adjustments. Then tick the Saturation and Color Filter checkboxes.");
                return;
            }

            // Force overrideState true so URP uses our runtime values.
            // If these are already ticked in the Inspector that's fine — setting true again is safe.
            _colorAdjustments.saturation.overrideState  = true;
            _colorAdjustments.colorFilter.overrideState = true;

            _volumeReady = true;
            Debug.Log("[EcologicalHealthVolume] ColorAdjustments ready. " +
                      "Ecological health visuals active.");
        }

        /// <summary>
        /// Applies the current display health value to the Volume parameters.
        /// Called every frame during the smooth lerp transition.
        /// </summary>
        private void ApplyToVolume(float health)
        {
            float t = Mathf.Clamp01(health / 100f);

            _colorAdjustments.saturation.value  =
                Mathf.Lerp(_criticalSaturation, _healthySaturation, t);

            _colorAdjustments.colorFilter.value =
                Color.Lerp(_criticalColorFilter, _healthyColorFilter, t);
        }
    }
}
