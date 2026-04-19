// ─────────────────────────────────────────────────────────────────────────────
// GameEvents.cs
// Path: Assets/Scripts/Systems/GameEvents.cs
// Terra's Heart — Central static event bus.
//
// Step 5 addition: OnResourceCollected — fired by ResourceNode on E-key pickup.
// Consumed by CraftingMaterialInventory on DrMaria.
//
// TestLevel addition: OnThrowInput — fired by PlayerController on T key press.
// Consumed by ThrowController (future — BrineglowDescent session).
// ─────────────────────────────────────────────────────────────────────────────

using System;
using TerrasHeart.Scanner;
using TerrasHeart.Adaptations;
using TerrasHeart.Environment;

namespace TerrasHeart.Events
{
    public static class GameEvents
    {
        // ─── Scanner ──────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when Dr. Maria successfully completes a full scan.
        /// Consumed by: ResearchJournalManager, SpecimenInventory, BiomeHealthManager.
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

        public static void RaiseScanComplete(ScanResult result) => OnScanComplete?.Invoke(result);
        public static void RaiseScanBegin(IScannable target) => OnScanBegin?.Invoke(target);
        public static void RaiseScanInterrupted() => OnScanInterrupted?.Invoke();

        // ─── Adaptations ──────────────────────────────────────────────────────

        /// <summary>
        /// Raised when Dr. Maria crafts and unlocks a new adaptation.
        /// Consumed by: AdaptationUI (future), gate controllers.
        /// </summary>
        public static event Action<AdaptationSO> OnAdaptationUnlocked;

        public static void RaiseAdaptationUnlocked(AdaptationSO adaptation) =>
            OnAdaptationUnlocked?.Invoke(adaptation);

        // ─── World State ──────────────────────────────────────────────────────

        /// <summary>
        /// Raised when a biome's ecological health score changes.
        /// Args: biomeID (string), newHealthValue (float 0–100).
        /// Consumed by: EcologicalHealthVolume, BiologicalResourceNode depletion,
        ///              MusicLayerManager (future).
        /// </summary>
        public static event Action<string, float> OnBiomeHealthChanged;

        public static void RaiseBiomeHealthChanged(string biomeID, float newHealth) =>
            OnBiomeHealthChanged?.Invoke(biomeID, newHealth);

        // ─── Resources ────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when Dr. Maria picks up a resource node (E key).
        /// Args: nodeType (ResourceNodeType), amount (int — always 1 per pickup in Step 5).
        /// Consumed by: CraftingMaterialInventory on DrMaria.
        /// </summary>
        public static event Action<ResourceNodeType, int> OnResourceCollected;

        public static void RaiseResourceCollected(ResourceNodeType nodeType, int amount) =>
            OnResourceCollected?.Invoke(nodeType, amount);

        // ─── Player Actions ───────────────────────────────────────────────────

        /// <summary>
        /// Raised when the player presses the throw key (T).
        /// Consumed by: ThrowController (future — BrineglowDescent session).
        /// No arguments — ThrowController will determine what is thrown based
        /// on selected inventory item at the time of the event.
        /// </summary>
        public static event Action OnThrowInput;

        public static void RaiseThrowInput() => OnThrowInput?.Invoke();

        // ─── Crew (reserved) ─────────────────────────────────────────────────

        /// <summary>
        /// Raised when a crew member's morale value changes.
        /// Args: crewMemberName (string), newMorale (float 0–100).
        /// </summary>
        public static event Action<string, float> OnCrewMoraleChanged;

        public static void RaiseCrewMoraleChanged(string crewMemberName, float newMorale) =>
            OnCrewMoraleChanged?.Invoke(crewMemberName, newMorale);
    }
}