// ─────────────────────────────────────────────────────────────────────────────
// GameEvents.cs
// Path: Assets/Scripts/Systems/GameEvents.cs
// Terra's Heart — Central static event bus.
// Updated: OnAdaptationUnlocked now passes AdaptationSO directly (not string).
// ─────────────────────────────────────────────────────────────────────────────

using System;
using TerrasHeart.Scanner;
using TerrasHeart.Adaptations;

namespace TerrasHeart.Events
{
    public static class GameEvents
    {
        // ─── Scanner ──────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when Dr. Maria successfully completes a full scan.
        /// Consumed by: ResearchJournalManager, SpecimenInventory, WorldStateManager (future).
        /// </summary>
        public static event Action<ScanResult> OnScanComplete;

        /// <summary>
        /// Raised the moment the scanner beam locks onto a valid IScannable target.
        /// Consumed by: ScannerUI (future), creature reaction systems (future).
        /// </summary>
        public static event Action<IScannable> OnScanBegin;

        /// <summary>
        /// Raised when a scan in progress is interrupted.
        /// Consumed by: ScannerUI (future).
        /// </summary>
        public static event Action OnScanInterrupted;

        public static void RaiseScanComplete(ScanResult result)   => OnScanComplete?.Invoke(result);
        public static void RaiseScanBegin(IScannable target)      => OnScanBegin?.Invoke(target);
        public static void RaiseScanInterrupted()                  => OnScanInterrupted?.Invoke();

        // ─── Adaptations ──────────────────────────────────────────────────────

        /// <summary>
        /// Raised when Dr. Maria crafts and unlocks a new adaptation.
        /// Consumed by: AdaptationUI (future), MetroidvaniaGateController (future).
        /// </summary>
        public static event Action<AdaptationSO> OnAdaptationUnlocked;

        public static void RaiseAdaptationUnlocked(AdaptationSO adaptation) =>
            OnAdaptationUnlocked?.Invoke(adaptation);

        // ─── World State ──────────────────────────────────────────────────────

        /// <summary>
        /// Raised when a biome's ecological health score changes.
        /// Args: biomeID (string), newHealthValue (float 0–100).
        /// </summary>
        public static event Action<string, float> OnBiomeHealthChanged;

        public static void RaiseBiomeHealthChanged(string biomeID, float newHealth) =>
            OnBiomeHealthChanged?.Invoke(biomeID, newHealth);

        // ─── Crew (reserved) ─────────────────────────────────────────────────

        /// <summary>
        /// Raised when a crew member's morale value changes.
        /// Args: crewMemberName (string), newMoraleValue (float 0–100).
        /// </summary>
        public static event Action<string, float> OnCrewMoraleChanged;

        public static void RaiseCrewMoraleChanged(string crewMemberName, float newMorale) =>
            OnCrewMoraleChanged?.Invoke(crewMemberName, newMorale);
    }
}
