// ─────────────────────────────────────────────────────────────────────────────
// IScannable.cs
// Path: Assets/Scripts/Scanner/IScannable.cs
// Terra's Heart — Implement this interface on every GameObject that
// Dr. Maria can scan: creatures, flora, geology, Thari tech, Poseidon artefacts.
//
// DESIGN PILLAR: Science first — the scanner is the primary verb, not the sword.
// The IsAlive state directly determines specimen tier. Killing a creature before
// scanning caps it at Common. This makes science the mechanically optimal path.
// ─────────────────────────────────────────────────────────────────────────────

namespace TerrasHeart.Scanner
{
    /// <summary>
    /// Contract for any object that Dr. Maria's scanner can target.
    /// </summary>
    public interface IScannable
    {
        /// <summary>
        /// Whether this target is alive at the moment the scan completes.
        /// TRUE  → yields the AliveTier defined in CreatureDataSO (Rare or better).
        /// FALSE → yields the DeadTier defined in CreatureDataSO (always Common).
        /// This is the core science-first incentive: scan alive, get better specimens.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Returns the ScriptableObject data record for this target.
        /// Must never return null on a valid IScannable — if no data is assigned
        /// the ScannerController will abort and log a warning.
        /// </summary>
        CreatureDataSO GetData();

        /// <summary>
        /// Called the moment the scanner beam locks onto this target and the hold timer begins.
        /// Use for creature awareness animations, visual indicators, etc. (future).
        /// </summary>
        void OnScanBegin();

        /// <summary>
        /// Called when the hold timer completes and the scan succeeds.
        /// Use for post-scan creature reactions, collected-state visuals, etc. (future).
        /// </summary>
        void OnScanComplete();

        /// <summary>
        /// Called when the player releases the scan button before the timer completes,
        /// or when the target is destroyed/disabled mid-scan.
        /// Use to revert any "being scanned" visual state.
        /// </summary>
        void OnScanInterrupted();
    }
}
