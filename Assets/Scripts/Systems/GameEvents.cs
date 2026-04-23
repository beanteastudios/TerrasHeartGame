// ─────────────────────────────────────────────────────────────────────────────
// GameEvents.cs
// Path: Assets/Scripts/Systems/GameEvents.cs
// Terra's Heart — Central static event bus.
//
// Phase B Step 4 addition: OnPaletteInput — fired by PlayerController on
// digit keys 1 and 2. Consumed by GlowMantleAI and GlowMantleHUDController.
//
// BrineglowDescent addition: OnSlideStateChanged — fired by PlayerController
// when Dr. Maria enters or exits the slide state on a slope surface.
// Consumed by: animation system and camera system (production phase).
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
        /// </summary>
        public static event Action OnThrowInput;

        public static void RaiseThrowInput() => OnThrowInput?.Invoke();

        /// <summary>
        /// Raised by FoodMarker on Start() after it is placed in the world.
        /// Args: worldPosition (Vector2) — the exact landing point.
        /// Consumed by: CaveLuminothAI, TarnCreeperAI.
        /// </summary>
        public static event Action<Vector2> OnFoodPlaced;

        // ─── Water ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when an object enters the water trigger and triggers a splash.
        /// Args: splashCenter (Vector2 world pos), radius (float), force (float).
        /// Future consumers: WaterSoundManager, BioluminescentFlashController, ScanTrigger.
        /// </summary>
        public static event Action<Vector2, float, float> OnWaterSplash;

        // <summary>
        /// Raised when DrMaria enters or exits the water volume trigger.
        /// Args: isSubmerged (bool) — true on entry, false on exit.
        /// Future consumers: AnimationController (swim state), AudioManager (underwater ambience).
        /// </summary>
        public static event Action<bool> OnPlayerSubmerged;

        public static void RaisePlayerSubmerged(bool isSubmerged) =>
            OnPlayerSubmerged?.Invoke(isSubmerged);

        public static void RaiseWaterSplash(Vector2 center, float radius, float force) =>
            OnWaterSplash?.Invoke(center, radius, force);

        public static void RaiseFoodPlaced(Vector2 position) => OnFoodPlaced?.Invoke(position);

        /// <summary>
        /// Raised when the player presses a palette key (1 or 2) for the
        /// Glow-Mantle call-and-response encounter.
        /// Args: index (int) — 0 = Cyan, 1 = Amber.
        /// Consumed by: GlowMantleAI (sequence validation),
        ///              GlowMantleHUDController (swatch flash).
        /// Always raised on key press — GlowMantleAI filters by state.
        /// </summary>
        public static event Action<int> OnPaletteInput;

        public static void RaisePaletteInput(int index) => OnPaletteInput?.Invoke(index);

        /// <summary>
        /// Raised when Dr. Maria enters or exits the slide state on a slope surface.
        /// Args: isSliding (bool) — true = slide entered, false = slide exited.
        /// Consumed by: animation system and camera system (production phase).
        /// </summary>
        public static event Action<bool> OnSlideStateChanged;

        public static void RaiseSlideStateChanged(bool isSliding) =>
            OnSlideStateChanged?.Invoke(isSliding);

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