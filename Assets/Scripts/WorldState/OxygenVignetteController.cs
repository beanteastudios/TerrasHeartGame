using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TerrasHeart.Systems;
using TerrasHeart.Player;
using TerrasHeart.Events;

namespace TerrasHeart.World
{
    /// <summary>
    /// Drives the URP Vignette post-process effect on the Global Volume based on oxygen level.
    /// Attach to the SAME GameObject as EcologicalHealthVolume (the Global Volume).
    ///
    /// SETUP REQUIREMENT: The Global Volume profile must have a Vignette override added.
    ///   Inspector → Global Volume profile → Add Override → Post-processing → Vignette.
    ///   Vignette intensity must have its override checkbox ticked.
    ///
    /// Subscribes to: OnOxygenChanged, OnOxygenCritical, OnSwimStateChanged
    ///
    /// DESIGN RULE: oxygen is NEVER shown as a UI bar or percentage.
    /// This vignette IS the feedback — it must be readable, not decorative.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class OxygenVignetteController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private OxygenConfigSO _oxygenConfig;

        [Header("Vignette Range")]
        [Tooltip("Vignette intensity when oxygen is full. Slight ambient darkening.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _baseIntensity = 0.1f;

        [Tooltip("Vignette intensity when oxygen is zero. Almost fills the screen.")]
        [Range(0.3f, 1f)]
        [SerializeField] private float _maxIntensity = 0.65f;

        [Header("Critical Pulse")]
        [Tooltip("Amplitude of the sine-wave pulse added to intensity at critical threshold.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _pulseAmplitude = 0.08f;

        [Tooltip("Speed of the pulse oscillation when critical.")]
        [SerializeField] private float _pulseSpeed = 2.5f;

        // ─────────────────────────────────────────────────────────────
        // REFERENCES
        // ─────────────────────────────────────────────────────────────

        private Volume   _volume;
        private Vignette _vignette;

        // ─────────────────────────────────────────────────────────────
        // STATE
        // ─────────────────────────────────────────────────────────────

        private float _targetIntensity;
        private bool  _isCritical;
        private bool  _isUnderwater;

        // ─────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _volume = GetComponent<Volume>();
            if (!_volume.profile.TryGet(out _vignette))
            {
                Debug.LogWarning(
                    "[OxygenVignetteController] No Vignette override found in the Global Volume profile. " +
                    "Add one via: Volume profile → Add Override → Post-processing → Vignette.", this);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnOxygenChanged   += HandleOxygenChanged;
            GameEvents.OnOxygenCritical  += HandleOxygenCritical;
            GameEvents.OnSwimStateChanged += HandleSwimStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnOxygenChanged   -= HandleOxygenChanged;
            GameEvents.OnOxygenCritical  -= HandleOxygenCritical;
            GameEvents.OnSwimStateChanged -= HandleSwimStateChanged;
        }

        // ─────────────────────────────────────────────────────────────
        // UPDATE — apply intensity each frame
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_vignette == null) return;

            float intensity = _targetIntensity;

            if (_isCritical && _isUnderwater)
            {
                // Pulse on top of base intensity at critical threshold
                intensity += Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmplitude;
            }

            _vignette.intensity.value = Mathf.Clamp01(intensity);
        }

        // ─────────────────────────────────────────────────────────────
        // EVENT HANDLERS
        // ─────────────────────────────────────────────────────────────

        private void HandleOxygenChanged(float normalisedOxygen)
        {
            // Drive intensity: full oxygen = base, zero oxygen = max
            _targetIntensity = Mathf.Lerp(_maxIntensity, _baseIntensity, normalisedOxygen);

            // Auto-clear critical flag when oxygen recovers above threshold
            if (_oxygenConfig != null)
            {
                float critNorm = _oxygenConfig.CriticalThreshold / _oxygenConfig.MaxOxygen;
                if (normalisedOxygen > critNorm)
                    _isCritical = false;
            }
        }

        private void HandleOxygenCritical()
        {
            _isCritical = true;
        }

        private void HandleSwimStateChanged(bool isSwimming)
        {
            _isUnderwater = isSwimming;

            if (!isSwimming)
            {
                // Clear vignette immediately on surfacing — relief should be tactile
                _isCritical      = false;
                _targetIntensity = 0f;

                if (_vignette != null)
                    _vignette.intensity.value = 0f;
            }
        }
    }
}
