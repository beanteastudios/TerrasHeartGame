// ─────────────────────────────────────────────────────────────────────────────
// BiomeHealthManager.cs
// Path: Assets/Scripts/WorldState/BiomeHealthManager.cs
// Terra's Heart — Owns the ecological health float for one biome.
// Subscribes to scan events and fires OnBiomeHealthChanged when health changes.
//
// Attach to: GameManagers (same GameObject as ResearchJournalManager)
//
// This is the "world remembers neglect" pillar in code.
// Every scan decision — alive vs dead — has a real consequence here.
// EcologicalHealthVolume listens to OnBiomeHealthChanged and drives URP visuals.
//
// One BiomeHealthManager per scene. In Phase 1, biome transitions will
// either carry or reset health state via the save system.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Events;
using TerrasHeart.Scanner;

namespace TerrasHeart.WorldState
{
    public class BiomeHealthManager : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Assign the BiomeHealthConfig SO for this scene's biome.")]
        [SerializeField] private BiomeHealthConfigSO _config;

        // ─── Runtime State ────────────────────────────────────────────────────

        private float _currentHealth;

        // ─── Public read access ───────────────────────────────────────────────

        /// <summary>Current biome health (0–100). Read by EcologicalHealthVolume.</summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>Health normalised 0–1. Convenient for Lerp calls in visual systems.</summary>
        public float HealthNormalised => _currentHealth / 100f;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogWarning("[BiomeHealthManager] No BiomeHealthConfigSO assigned. " +
                                 "Defaulting to 100% health.");
                _currentHealth = 100f;
                return;
            }

            _currentHealth = _config.StartingHealth;
        }

        private void OnEnable()
        {
            GameEvents.OnScanComplete += HandleScanComplete;
        }

        private void OnDisable()
        {
            GameEvents.OnScanComplete -= HandleScanComplete;
        }

        private void Start()
        {
            // Fire initial health event so EcologicalHealthVolume sets the
            // correct visual state at scene load — not just on first scan.
            FireHealthEvent();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Scan Event Handler
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reacts to every completed scan.
        /// Alive scan → small restoration (studying without harming).
        /// Dead scan → health penalty (ecological cost already paid).
        ///
        /// Only reacts to scans from this biome — ignores scans from other zones.
        /// </summary>
        private void HandleScanComplete(ScanResult result)
        {
            if (_config == null || result?.SourceData == null) return;

            // Only react to creatures scanned in this biome
            if (result.SourceData.BiomeID != _config.BiomeID) return;

            if (result.WasAlive)
            {
                ApplyChange(_config.ScanRestoration, "alive scan");
            }
            else
            {
                ApplyChange(-_config.KillPenalty, "dead scan");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API — for future combat system
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Call when Dr. Maria kills a healthy creature in this biome
        /// without scanning it first. Full kill penalty applied.
        /// </summary>
        public void ApplyKillPenalty()
        {
            if (_config == null) return;
            ApplyChange(-_config.KillPenalty, "creature killed unscanned");
        }

        /// <summary>
        /// Call when Dr. Maria tames and scans a corrupted creature.
        /// Larger restoration reward — meaningful ecological act.
        /// </summary>
        public void ApplyCorruptedRestoration()
        {
            if (_config == null) return;
            ApplyChange(_config.CorruptedRestoration, "corrupted creature restored");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void ApplyChange(float delta, string reason)
        {
            float previous = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth + delta, 0f, 100f);

            Debug.Log($"[BiomeHealth] {_config.BiomeID}: {previous:F1} → {_currentHealth:F1} " +
                      $"({(delta >= 0 ? "+" : "")}{delta:F1} — {reason})");

            FireHealthEvent();
        }

        private void FireHealthEvent()
        {
            if (_config == null) return;
            GameEvents.RaiseBiomeHealthChanged(_config.BiomeID, _currentHealth);
        }
    }
}
