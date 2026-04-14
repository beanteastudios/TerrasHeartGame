// ─────────────────────────────────────────────────────────────────────────────
// JournalEntry.cs
// Path: Assets/Scripts/Scanner/JournalEntry.cs
// Terra's Heart — Immutable record of a single scan logged to the Research Journal.
//
// Stored in ResearchJournalManager._entries.
// Future: drives the Species Log tab of the Research Journal UI.
// Dr. Maria's FieldNote is her voice — written in first person on CreatureDataSO.
// ─────────────────────────────────────────────────────────────────────────────

namespace TerrasHeart.Scanner
{
    /// <summary>
    /// One entry in Dr. Maria's Research Journal.
    /// Produced from a ScanResult when ResearchJournalManager receives OnScanComplete.
    /// </summary>
    public class JournalEntry
    {
        /// <summary>Species display name from CreatureDataSO.</summary>
        public string SpeciesName { get; }

        /// <summary>Dr. Maria's field note — written in her voice on CreatureDataSO.</summary>
        public string FieldNote { get; }

        /// <summary>The biome this specimen was collected from.</summary>
        public string BiomeID { get; }

        /// <summary>Specimen quality tier at time of scan.</summary>
        public SpecimenTier Tier { get; }

        /// <summary>Whether the target was alive when scanned.</summary>
        public bool WasAlive { get; }

        /// <summary>Time.time value when the scan completed.</summary>
        public float Timestamp { get; }

        public JournalEntry(
            string speciesName,
            string fieldNote,
            string biomeID,
            SpecimenTier tier,
            bool wasAlive,
            float timestamp)
        {
            SpeciesName = speciesName;
            FieldNote   = fieldNote;
            BiomeID     = biomeID;
            Tier        = tier;
            WasAlive    = wasAlive;
            Timestamp   = timestamp;
        }
    }
}
