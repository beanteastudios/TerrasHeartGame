// ─────────────────────────────────────────────────────────────────────────────
// GameEvents.cs
// Path: Assets/Scripts/Systems/GameEvents.cs
// Terra's Heart — Central static event bus.
//
// Step 5 addition: OnResourceCollected — fired by ResourceNode on E-key pickup.
// Consumed by CraftingMaterialInventory on DrMaria.
//
// TestLevel addition: OnThrowInput — fired by PlayerController on T key press.
// Consumed by ThrowController.
//
// Phase B Step 2 addition: OnFoodPlaced — fired by FoodMarker on Start().
// Consumed by CaveLuminothAI (comfort trigger), TarnCreeperAI (pre-placement tier).
// ─────────────────────────────────────────────────────────────────────────────

using System;
using TerrasHeart.Scanner;
using TerrasHeart.Adaptations;
using TerrasHeart.Environment;
using UnityEngine;

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
        /// Args: nodeType (ResourceNodeType), amount (int — always 1 per pickup).
        /// Consumed by: CraftingMaterialInventory on DrMaria.
        /// </summary>
        public static event Action<ResourceNodeType, int> OnResourceCollected;

        public static void RaiseResourceCollected(ResourceNodeType nodeType, int amount) =>
            OnResourceCollected?.Invoke(nodeType, amount);

        // ─── Player Actions ───────────────────────────────────────────────────

        /// <summary>
        /// Raised when the player presses the throw key (T).
        /// Consumed by: ThrowController on DrMaria.
        /// No arguments — ThrowController determines what is thrown based on
        /// selected inventory item at the time of the event.
        /// </summary>
        public static event Action OnThrowInput;

        public static void RaiseThrowInput() => OnThrowInput?.Invoke();

        /// <summary>
        /// Raised by FoodMarker on Start() after it is placed in the world.
        /// Args: worldPosition (Vector2) — the exact landing point.
        /// Consumed by: CaveLuminothAI (frighten vs comfort check),
        ///              TarnCreeperAI (pre-placement tier detection).
        /// Creature AI scripts check distance from this position to determine
        /// their reaction — frighten radius or comfort range.
        /// </summary>
        public static event Action<Vector2> OnFoodPlaced;

        public static void RaiseFoodPlaced(Vector2 position) => OnFoodPlaced?.Invoke(position);

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