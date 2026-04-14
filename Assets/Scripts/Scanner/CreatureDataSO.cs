// ─────────────────────────────────────────────────────────────────────────────
// CreatureDataSO.cs
// Path: Assets/Scripts/Scanner/CreatureDataSO.cs
// Terra's Heart — Per-species data record. One SO per scannable species.
//
// Create via: Assets > Create > TerrasHeart > Scanner > CreatureData
// Save to:    Assets/Data/ScriptableObjects/Creatures/
//
// The FieldNote is Dr. Maria's voice — write it in first person present tense,
// as if she's making the observation in real time. This text appears verbatim
// in the Research Journal Species Log tab.
//
// AliveTier vs DeadTier gap is the mechanical enforcement of "science first":
// scanning alive rewards Rare+, scanning dead yields only Common.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace TerrasHeart.Scanner
{
    [CreateAssetMenu(fileName = "NewCreatureData", menuName = "TerrasHeart/Scanner/CreatureData")]
    public class CreatureDataSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name shown in the Research Journal. " +
                 "Use the scientific/Thari name with a colloquial note if needed. " +
                 "Example: 'Lumifin Ray (Thari: Vel-Aur)'")]
        [SerializeField] private string _speciesName;

        [Tooltip("Biome this creature originates from. Must match the BiomeID used in WorldStateManager. " +
                 "Example: 'ShallowCaves', 'WaterfallChambers', 'DeepRiver'")]
        [SerializeField] private string _biomeID;

        [Header("Research Journal")]
        [Tooltip("Dr. Maria's field note. First person, present tense. " +
                 "Appears verbatim in the Species Log tab. Write in her voice — " +
                 "marine biologist, climate-aware, wonder and dread in equal measure.")]
        [TextArea(3, 8)]
        [SerializeField] private string _fieldNote;

        [Header("Specimen Tiers")]
        [Tooltip("Specimen tier yielded when scanned alive. " +
                 "Set to Rare or higher for most creatures — rewards science-first play. " +
                 "Exceptional or AurTouched reserved for rare/significant species.")]
        [SerializeField] private SpecimenTier _aliveTier = SpecimenTier.Rare;

        [Tooltip("Specimen tier yielded when scanned dead. " +
                 "Should always be Common — this is the ecological penalty for killing before scanning. " +
                 "Only change this if a species has no living scan equivalent by design.")]
        [SerializeField] private SpecimenTier _deadTier = SpecimenTier.Common;

        [Header("Scan Behaviour")]
        [Tooltip("Multiplier applied to ScannerConfigSO.ScanHoldDuration for this specific species. " +
                 "1.0 = standard. Use > 1.0 for elusive/wary creatures (rewards patience). " +
                 "Use < 1.0 for passive or slow creatures that are easy to scan.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _scanDurationMultiplier = 1f;

        // ─── Public API ───────────────────────────────────────────────────────

        public string SpeciesName              => _speciesName;
        public string BiomeID                  => _biomeID;
        public string FieldNote                => _fieldNote;
        public SpecimenTier AliveTier          => _aliveTier;
        public SpecimenTier DeadTier           => _deadTier;
        public float ScanDurationMultiplier    => _scanDurationMultiplier;

        /// <summary>
        /// Returns the correct specimen tier based on the target's alive state at scan completion.
        /// This is the single point where the alive/dead tier decision is made.
        /// </summary>
        public SpecimenTier GetTierFor(bool isAlive) => isAlive ? _aliveTier : _deadTier;
    }
}
