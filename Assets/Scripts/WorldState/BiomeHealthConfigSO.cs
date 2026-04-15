// ─────────────────────────────────────────────────────────────────────────────
// BiomeHealthConfigSO.cs
// Path: Assets/Scripts/WorldState/BiomeHealthConfigSO.cs
// Terra's Heart — Configuration data for one biome's ecological health system.
//
// Create via: Assets > Create > TerrasHeart > WorldState > BiomeHealthConfig
// Save to:    Assets/Data/ScriptableObjects/WorldState/
//
// One SO per biome. The prologue scene uses a "Prologue" config for testing.
// Shallow Caves, Waterfall Chambers etc. each get their own config in Phase 1.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace TerrasHeart.WorldState
{
    [CreateAssetMenu(fileName = "NewBiomeHealthConfig",
                     menuName = "TerrasHeart/WorldState/BiomeHealthConfig")]
    public class BiomeHealthConfigSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique ID for this biome. Must match the BiomeID on CreatureDataSO entries " +
                 "and the string used in GameEvents.OnBiomeHealthChanged. " +
                 "Example: 'Prologue', 'ShallowCaves', 'WaterfallChambers'")]
        [SerializeField] private string _biomeID = "Prologue";

        [Header("Starting State")]
        [Tooltip("Health value when the biome is first entered. 0–100. " +
                 "Shallow Caves starts at 75 (already stressed by agricultural runoff). " +
                 "Prologue scene test value: 80.")]
        [Range(0f, 100f)]
        [SerializeField] private float _startingHealth = 80f;

        [Header("Health Change Amounts")]
        [Tooltip("Health lost when a creature is killed (dead scan or not scanned at all). " +
                 "Represents permanent ecological damage. Default: 10.")]
        [Range(0f, 50f)]
        [SerializeField] private float _killPenalty = 10f;

        [Tooltip("Health restored when a creature is scanned alive. " +
                 "Scanning alive means studying without harming — a small restoration reward. " +
                 "Default: 3. Intentionally less than the kill penalty.")]
        [Range(0f, 20f)]
        [SerializeField] private float _scanRestoration = 3f;

        [Tooltip("Health restored when a corrupted creature is tamed and scanned. " +
                 "Larger reward — restoring a corrupted creature is a meaningful ecological act. " +
                 "Default: 15. Implemented in Phase 1 combat encounters.")]
        [Range(0f, 50f)]
        [SerializeField] private float _corruptedRestoration = 15f;

        // ─── Public API ───────────────────────────────────────────────────────

        public string BiomeID              => _biomeID;
        public float StartingHealth        => _startingHealth;
        public float KillPenalty           => _killPenalty;
        public float ScanRestoration       => _scanRestoration;
        public float CorruptedRestoration  => _corruptedRestoration;
    }
}
