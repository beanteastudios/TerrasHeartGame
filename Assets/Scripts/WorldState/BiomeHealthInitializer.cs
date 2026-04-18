// ─────────────────────────────────────────────────────────────────────────────
// BiomeHealthInitializer.cs
// Path: Assets/Scripts/WorldState/BiomeHealthInitializer.cs
// Terra's Heart — Scene-local component that assigns the correct
// BiomeHealthConfigSO to the persistent BiomeHealthManager on scene load.
//
// WHY THIS EXISTS:
//   GameManagers persists across scenes via PersistentEntity, which means
//   BiomeHealthManager carries the previous scene's config into the new scene.
//   This component lives in each scene (not persistent), finds the persistent
//   BiomeHealthManager on Start, and hot-swaps the config for this scene.
//
// SETUP:
//   Place on any GameObject in the scene _Managers hierarchy.
//   Assign the correct BiomeHealthConfigSO for this scene.
//   Do NOT add PersistentEntity to this GameObject.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.WorldState;

namespace TerrasHeart.WorldState
{
    public class BiomeHealthInitializer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Assign the BiomeHealthConfigSO for this scene. " +
                 "BrineglowDescent scene: assign BrineglowDescentHealth.asset. " +
                 "MeridianBase scene: leave empty — no biome health in base camp.")]
        [SerializeField] private BiomeHealthConfigSO _config;

        private void Start()
        {
            if (_config == null)
            {
                Debug.Log("[BiomeHealthInitializer] No config assigned — " +
                          "biome health inactive in this scene.");
                return;
            }

            // Find the persistent BiomeHealthManager on GameManagers
            BiomeHealthManager manager = FindFirstObjectByType<BiomeHealthManager>();

            if (manager == null)
            {
                Debug.LogWarning("[BiomeHealthInitializer] BiomeHealthManager not found. " +
                                 "Ensure GameManagers is persistent via PersistentEntity.");
                return;
            }

            manager.InitialiseForScene(_config);

            Debug.Log($"[BiomeHealthInitializer] BiomeHealthManager configured for: " +
                      $"{_config.BiomeID} (starting health: {_config.StartingHealth})");
        }
    }
}
