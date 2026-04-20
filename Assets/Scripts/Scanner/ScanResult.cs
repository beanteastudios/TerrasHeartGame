// ─────────────────────────────────────────────────────────────────────────────
// ScanResult.cs
// Path: Assets/Scripts/Scanner/ScanResult.cs
// Terra's Heart — Immutable runtime data produced by one completed scan.
//
// NOT a ScriptableObject — transient per-scan data, not static game data.
// Built by ScannerController, fired via GameEvents.OnScanComplete,
// consumed by ResearchJournalManager, SpecimenInventory, BiomeHealthManager.
//
// Phase B Step 1 — Bug fix:
//   IsRestoredCorrupted added. Set by ScannerController when the scanned target
//   implements ICorruptedScannable. BiomeHealthManager reads this flag to apply
//   CorruptedRestoration instead of ScanRestoration — routing all health
//   changes through one path.
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
        /// FALSE = ecological cost was already paid (creature was killed before scanning).
        /// </summary>
        public bool WasAlive { get; }

        /// <summary>Time.time value when the scan completed. Used for journal timestamps.</summary>
        public float Timestamp { get; }

        /// <summary>
        /// True when the scanned target implemented ICorruptedScannable — i.e., this scan
        /// was the tame-and-scan completion on a restored corrupted creature.
        /// BiomeHealthManager uses this flag to apply CorruptedRestoration instead of
        /// ScanRestoration, routing all health changes through HandleScanComplete.
        /// Default false — all non-corrupted scans leave this unset.
        /// </summary>
        public bool IsRestoredCorrupted { get; }

        public ScanResult(
            CreatureDataSO sourceData,
            SpecimenTier tier,
            bool wasAlive,
            float timestamp,
            bool isRestoredCorrupted = false)
        {
            SourceData = sourceData;
            Tier = tier;
            WasAlive = wasAlive;
            Timestamp = timestamp;
            IsRestoredCorrupted = isRestoredCorrupted;
        }
    }
}