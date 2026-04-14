// ─────────────────────────────────────────────────────────────────────────────
// BlueprintSO.cs
// Path: Assets/Scripts/Adaptations/BlueprintSO.cs
// Terra's Heart — Crafting recipe that converts specimens into an adaptation.
// One SO per craftable adaptation. Designed by Callum, enabled by scan data.
//
// The minimum tier requirement enforces science-first: a dead-scanned Common
// specimen cannot unlock a Rare-gated adaptation. Killing before scanning
// produces inferior specimens that cannot meet blueprint requirements.
//
// Create via: Assets > Create > TerrasHeart > Adaptations > Blueprint
// Save to:    Assets/Data/ScriptableObjects/Blueprints/
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Scanner;

namespace TerrasHeart.Adaptations
{
    [CreateAssetMenu(fileName = "NewBlueprint", menuName = "TerrasHeart/Adaptations/Blueprint")]
    public class BlueprintSO : ScriptableObject
    {
        [Header("Output")]
        [Tooltip("The adaptation this blueprint unlocks when crafted.")]
        [SerializeField] private AdaptationSO _yieldsAdaptation;

        [Header("Requirements")]
        [Tooltip("Which species specimen is required. Must match a CreatureDataSO in the player's inventory.")]
        [SerializeField] private CreatureDataSO _requiredCreature;

        [Tooltip("Minimum specimen tier required. Common specimens cannot meet a Rare requirement. " +
                 "This is the mechanical cost of killing before scanning.")]
        [SerializeField] private SpecimenTier _minimumTier = SpecimenTier.Rare;

        [Tooltip("How many specimens of the required species and tier are consumed on craft.")]
        [SerializeField] private int _requiredCount = 1;

        // ─── Public API ───────────────────────────────────────────────────────

        public AdaptationSO YieldsAdaptation => _yieldsAdaptation;
        public CreatureDataSO RequiredCreature => _requiredCreature;
        public SpecimenTier MinimumTier       => _minimumTier;
        public int RequiredCount              => _requiredCount;
    }
}
