using UnityEngine;
using TerrasHeart.Systems;
using TerrasHeart.Events;

namespace TerrasHeart.Player
{
    /// <summary>
    /// Manages Dr. Maria's oxygen supply underwater.
    /// Attach to DrMaria GameObject alongside PlayerController.
    ///
    /// Subscribes to: OnSwimStateChanged
    /// Raises: OnOxygenChanged (normalised 0–1), OnOxygenCritical, OnOxygenDepleted
    ///
    /// UI rule: oxygen is NEVER shown as a bar or percentage.
    /// OxygenVignetteController drives the URP vignette based on OnOxygenChanged.
    /// </summary>
    public class OxygenManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private OxygenConfigSO _config;

        // ─────────────────────────────────────────────────────────────
        // STATE
        // ─────────────────────────────────────────────────────────────

        private float _currentOxygen;
        private bool _isUnderwater;

        // One-shot flags reset when surfacing — prevent event spam
        private bool _criticalRaised;
        private bool _depletedRaised;

        // ─────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError("[OxygenManager] OxygenConfigSO not assigned. Assign it in the Inspector.", this);
                return;
            }
            _currentOxygen = _config.MaxOxygen;
        }

        private void OnEnable()  => GameEvents.OnSwimStateChanged += HandleSwimStateChanged;
        private void OnDisable() => GameEvents.OnSwimStateChanged -= HandleSwimStateChanged;

        // ─────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_config == null) return;

            if (_isUnderwater)
                _currentOxygen -= _config.DrainRate * Time.deltaTime;
            else
                _currentOxygen += _config.RefillRate * Time.deltaTime;

            _currentOxygen = Mathf.Clamp(_currentOxygen, 0f, _config.MaxOxygen);

            float normalised = _currentOxygen / _config.MaxOxygen;
            GameEvents.RaiseOxygenChanged(normalised);

            CheckThresholds();
        }

        // ─────────────────────────────────────────────────────────────
        // THRESHOLD CHECKS
        // ─────────────────────────────────────────────────────────────

        private void CheckThresholds()
        {
            // Critical — raised once per dive
            if (_currentOxygen <= _config.CriticalThreshold && !_criticalRaised)
            {
                _criticalRaised = true;
                GameEvents.RaiseOxygenCritical();
            }

            // Depleted — raised once per dive
            if (_currentOxygen <= 0f && !_depletedRaised)
            {
                _depletedRaised = true;
                GameEvents.RaiseOxygenDepleted();
            }

            // Health drain stub — fires every frame while at zero
            if (_currentOxygen <= 0f && _isUnderwater)
            {
                // TODO: route to HealthManager when it exists
                Debug.Log($"[OxygenManager] Health draining at {_config.HealthDrainRate} HP/s. (HealthManager stub)");
            }
        }

        // ─────────────────────────────────────────────────────────────
        // EVENT HANDLERS
        // ─────────────────────────────────────────────────────────────

        private void HandleSwimStateChanged(bool isSwimming)
        {
            _isUnderwater = isSwimming;

            if (!isSwimming)
            {
                // Reset one-shot flags on surface — next dive starts clean
                _criticalRaised = false;
                _depletedRaised = false;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PUBLIC ACCESSORS
        // ─────────────────────────────────────────────────────────────

        /// <summary>Returns current oxygen normalised to 0–1. Used by UI tests only — never show raw value in game UI.</summary>
        public float GetNormalisedOxygen() =>
            _config != null ? _currentOxygen / _config.MaxOxygen : 1f;
    }
}
