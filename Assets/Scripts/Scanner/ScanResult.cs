// ─────────────────────────────────────────────────────────────────────────────
// ScanResult.cs
// Path: Assets/Scripts/Scanner/ScanResult.cs
// Terra's Heart — Immutable runtime data produced by one completed scan.
//
// NOT a ScriptableObject — this is transient per-scan data, not static game data.
// Built by ScannerController, fired through GameEvents.OnScanComplete,
// then consumed by ResearchJournalManager and (future) WorldStateManager.
// ─────────────────────────────────────────────────────────────────────────────

namespace TerrasHeart.Scanner
{
    /// <summary>
    /// Immutable snapshot of data produced by one successful scan.
    /// Created in ScannerController.CompleteScan(), raised via GameEvents.RaiseScanComplete().
    /// </summary>
    public class ScanResult
    {
        /// <summary>The ScriptableObject definition for the scanned species.</summary>
        public CreatureDataSO SourceData { get; }

        /// <summary>
        /// The specimen tier yielded by this scan.
        /// Determined by IsAlive state at moment of scan completion.
        /// </summary>
        public SpecimenTier Tier { get; }

        /// <summary>
        /// Whether the target was alive when the scan completed.
        /// FALSE = ecological cost was already paid (killing happened before scanning).
        /// </summary>
        public bool WasAlive { get; }

        /// <summary>Time.time value when the scan completed. Used for journal timestamps.</summary>
        public float Timestamp { get; }

        public ScanResult(CreatureDataSO sourceData, SpecimenTier tier, bool wasAlive, float timestamp)
        {
            SourceData = sourceData;
            Tier       = tier;
            WasAlive   = wasAlive;
            Timestamp  = timestamp;
        }
    }
}
