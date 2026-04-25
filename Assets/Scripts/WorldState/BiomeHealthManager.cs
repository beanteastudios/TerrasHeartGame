using System.Collections.Generic;
using UnityEngine;
using TerrasHeart.Events;
using TerrasHeart.Scanner;
using TerrasHeart.WorldState;

namespace TerrasHeart.World
{
    /// <summary>
    /// Single authority for biome health. Manages health floats (0–100) per biome.
    /// All health changes route through this manager — nothing calls it directly.
    ///
    /// INPUTS:
    ///   OnScanComplete       → scan restoration or corrupted restoration
    ///   OnBiomeHealthDelta   → environmental delta requests (stalactites, hazards)
    ///
    /// OUTPUT:
    ///   OnBiomeHealthChanged (biomeID, newHealth)
    ///   → consumed by EcologicalHealthVolume, BiomeVisualController
    ///
    /// InitialiseForScene() is called by BiomeHealthInitializer on each scene load
    /// to hot-swap the config without destroying the persistent manager.
    /// </summary>
    public class BiomeHealthManager : MonoBehaviour
    {
        [Header("Biome Configs")]
        [Tooltip("Add one BiomeHealthConfigSO per biome. BrineglowDescentHealth.asset for the vertical slice.")]
        [SerializeField] private List<BiomeHealthConfigSO> _biomeConfigs;

        // ─────────────────────────────────────────────────────────────
        // INTERNAL MAPS
        // ─────────────────────────────────────────────────────────────

        private readonly Dictionary<string, float> _healthMap = new();
        private readonly Dictionary<string, BiomeHealthConfigSO> _configMap = new();

        // ─────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            foreach (var config in _biomeConfigs)
            {
                if (config == null) continue;
                RegisterConfig(config);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnScanComplete += HandleScanComplete;
            GameEvents.OnBiomeHealthDelta += HandleBiomeHealthDelta;
        }

        private void OnDisable()
        {
            GameEvents.OnScanComplete -= HandleScanComplete;
            GameEvents.OnBiomeHealthDelta -= HandleBiomeHealthDelta;
        }

        // ─────────────────────────────────────────────────────────────
        // SCENE INITIALISATION
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by BiomeHealthInitializer on scene load to register or reset
        /// the config for the current scene's biome. Safe to call multiple times —
        /// will not reset health if the biome is already tracked (preserves
        /// ecological state across scene transitions within the same session).
        /// </summary>
        public void InitialiseForScene(BiomeHealthConfigSO config)
        {
            if (config == null) return;

            // Only register if not already tracked — preserves health across revisits
            if (!_configMap.ContainsKey(config.BiomeID))
            {
                RegisterConfig(config);
                Debug.Log($"[BiomeHealthManager] Registered new biome: {config.BiomeID} " +
                          $"at {config.StartingHealth}%");
            }
            else
            {
                Debug.Log($"[BiomeHealthManager] Biome already tracked: {config.BiomeID} " +
                          $"at {_healthMap[config.BiomeID]}% (state preserved)");
            }

            // Broadcast current health immediately so visual systems sync on scene load
            GameEvents.RaiseBiomeHealthChanged(config.BiomeID, _healthMap[config.BiomeID]);
        }

        // ─────────────────────────────────────────────────────────────
        // INTERNAL REGISTRATION
        // ─────────────────────────────────────────────────────────────

        private void RegisterConfig(BiomeHealthConfigSO config)
        {
            _healthMap[config.BiomeID] = config.StartingHealth;
            _configMap[config.BiomeID] = config;
        }

        // ─────────────────────────────────────────────────────────────
        // SCAN COMPLETE
        // ─────────────────────────────────────────────────────────────

        public void HandleScanComplete(ScanResult result)
        {
            if (result == null) return;

            string biomeID = result.SourceData?.BiomeID;
            if (string.IsNullOrEmpty(biomeID)) return;
            if (!_configMap.TryGetValue(biomeID, out var config)) return;

            float delta = result.IsRestoredCorrupted
                ? config.CorruptedRestoration
                : config.ScanRestoration;

            ApplyDelta(biomeID, delta);
        }

        // ─────────────────────────────────────────────────────────────
        // BIOME HEALTH DELTA (environmental sources)
        // ─────────────────────────────────────────────────────────────

        private void HandleBiomeHealthDelta(string biomeID, float delta)
        {
            ApplyDelta(biomeID, delta);
        }

        // ─────────────────────────────────────────────────────────────
        // APPLY DELTA
        // ─────────────────────────────────────────────────────────────

        private void ApplyDelta(string biomeID, float delta)
        {
            if (!_healthMap.ContainsKey(biomeID))
            {
                Debug.LogWarning($"[BiomeHealthManager] BiomeID '{biomeID}' not found in config map.", this);
                return;
            }

            _healthMap[biomeID] = Mathf.Clamp(_healthMap[biomeID] + delta, 0f, 100f);
            GameEvents.RaiseBiomeHealthChanged(biomeID, _healthMap[biomeID]);
        }

        // ─────────────────────────────────────────────────────────────
        // PUBLIC ACCESSOR
        // ─────────────────────────────────────────────────────────────

        /// <summary>Returns current health for a biome (0–100). Returns 0 if biomeID not found.</summary>
        public float GetHealth(string biomeID)
            => _healthMap.TryGetValue(biomeID, out float h) ? h : 0f;
    }
}