// ─────────────────────────────────────────────────────────────────────────────
// CrewMemberDataSO.cs
// Path: Assets/Scripts/Crew/CrewMemberDataSO.cs
// Terra's Heart — Static data for one crew member.
// Defines their skill bonuses per task type.
// One SO per crew member.
//
// Create via: Assets > Create > TerrasHeart > Crew > CrewMemberData
// Save to:    Assets/Data/ScriptableObjects/Crew/
//
// Skill bonuses are MULTIPLIERS applied to the base value:
//   < 1.0 = faster / better (e.g. 0.6 = 40% faster scan processing)
//   > 1.0 = slower / worse  (wrong person for the job)
//   1.0   = no effect       (crew member unassigned or neutral)
//
// Yuki on Research: ScanDurationMultiplier = 0.6 → scans complete 40% faster.
// This is the MVP proof: assign Yuki, feel scans get snappier.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace TerrasHeart.Crew
{
    [CreateAssetMenu(fileName = "NewCrewMemberData",
                     menuName = "TerrasHeart/Crew/CrewMemberData")]
    public class CrewMemberDataSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private CrewMemberID _id;
        [SerializeField] private string _displayName;

        [Header("Research Task Bonuses")]
        [Tooltip("Multiplier applied to ScannerConfigSO.ScanHoldDuration when this crew member " +
                 "is assigned to Research. 0.6 = 40% faster. 1.0 = no effect. " +
                 "Yuki: 0.6 (specialist). Others: 1.0 (no benefit).")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _scanDurationMultiplier = 1.0f;

        [Tooltip("Multiplier applied to specimen yield processing time. " +
                 "Future use — not consumed in MVP.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _specimenProcessingMultiplier = 1.0f;

        [Header("Crafting Task Bonuses")]
        [Tooltip("Multiplier applied to adaptation crafting time. " +
                 "Callum: 0.7 (specialist). Future use — not consumed in MVP.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _craftingSpeedMultiplier = 1.0f;

        [Header("Repair Task Bonuses")]
        [Tooltip("Multiplier applied to Meridian repair time. " +
                 "Leif: 0.65 (specialist). Future use — not consumed in MVP.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _repairSpeedMultiplier = 1.0f;

        [Header("Translation Task Bonuses")]
        [Tooltip("Multiplier applied to Thari translation progress rate. " +
                 "Yuki: 0.5 (specialist). Future use — not consumed in MVP.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _translationSpeedMultiplier = 1.0f;

        // ─── Public API ───────────────────────────────────────────────────────

        public CrewMemberID ID                    => _id;
        public string DisplayName                 => _displayName;
        public float ScanDurationMultiplier       => _scanDurationMultiplier;
        public float SpecimenProcessingMultiplier => _specimenProcessingMultiplier;
        public float CraftingSpeedMultiplier      => _craftingSpeedMultiplier;
        public float RepairSpeedMultiplier        => _repairSpeedMultiplier;
        public float TranslationSpeedMultiplier   => _translationSpeedMultiplier;
    }
}
