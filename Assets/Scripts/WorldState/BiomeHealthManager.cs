// ─────────────────────────────────────────────────────────────────────────────
// BiomeHealthManager.cs
// Path: Assets/Scripts/WorldState/BiomeHealthManager.cs
// Terra's Heart — Owns the ecological health float for one biome.
// Subscribes to scan events and fires OnBiomeHealthChanged when health changes.
//
// Attach to: GameManagers (same GameObject as ResearchJournalManager)
//
// This is the "world remembers neglect" pillar in code.
// Every scan decision — alive vs dead, restored vs standard — has a real
// consequence here. EcologicalHealthVolume listens to OnBiomeHealthChanged
// and drives URP colour grade visuals.
//
// Step 5 addition: InitialiseForScene() — called by BiomeHealthInitializer
// in each scene to hot-swap the config after GameManagers persists across loads.
//
// Phase B Step 1 — Bug fix:
//   HandleScanComplete now branches on ScanResult.IsRestoredCorrupted.
//   Corrupted creature restorations apply CorruptedRestoration (+15).
//   Standard alive scans apply ScanRestoration (+3).
//   Dead scans apply -KillPenalty.
//   All health changes route through this single handler — no direct calls
//   from creature AI scripts.
//   ApplyCorruptedRestoration() removed: it was called directly by TarnCreeperAI,
//   which caused double-counting. That direct reference has been removed.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Events;
using TerrasHeart.Scanner;

namespace TerrasHeart.WorldState
{
    public class BiomeHealthManager : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Assign the BiomeHealthConfig SO for this scene's biome. " +
                 "In Step 5 this is overridden at runtime by BiomeHealthInitializer " +
                 "when scenes are loaded — but assign PrologueHealth.asset here " +
                 "as the default for the Prologue scene.")]
        [SerializeField] private BiomeHealthConfigSO _config;

        // ─── Runtime State ────────────────────────────────────────────────────

        private float _currentHealth;

        // ─── Public Read Access ───────────────────────────────────────────────

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
            // Fire initial health event so EcologicalHealthVolume sets the correct
            // visual state at scene load — not just on first scan.
            FireHealthEvent();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Scene Initialisation — called by BiomeHealthInitializer
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Hot-swaps the biome config when a new scene loads.
        /// Called by BiomeHealthInitializer which lives in each scene.
        /// Resets health to the new config's starting value and fires the
        /// initial health event so EcologicalHealthVolume updates immediately.
        /// </summary>
        public void InitialiseForScene(BiomeHealthConfigSO config)
        {
            _config = config;
            _currentHealth = config.StartingHealth;
            FireHealthEvent();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Scan Event Handler — single source of truth for all scan-driven health
        // ─────────────────────────────────────────────────────────────────────

        private void HandleScanComplete(ScanResult result)
        {
            if (_config == null || result?.SourceData == null) return;

            // Only react to creatures scanned in this biome.
            if (result.SourceData.BiomeID != _config.BiomeID) return;

            if (result.IsRestoredCorrupted)
                // Tame-and-scan completion: larger restoration reward.
                ApplyChange(_config.CorruptedRestoration, "corrupted creature restored");
            else if (result.WasAlive)
                // Standard live scan: small ecological restoration.
                ApplyChange(_config.ScanRestoration, "alive scan");
            else
                // Dead scan or kill: ecological penalty.
                ApplyChange(-_config.KillPenalty, "dead scan");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Call when Dr. Maria kills a healthy creature in this biome without
        /// scanning it first. Separate from the scan event path — wired up
        /// when full combat kill detection is implemented.
        /// </summary>
        public void ApplyKillPenalty()
        {
            if (_config == null) return;
            ApplyChange(-_config.KillPenalty, "creature killed unscanned");
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