using System;
using UnityEngine;

namespace TerrasHeart.Events
{
    /// <summary>
    /// Static event bus for all cross-system communication in Terra's Heart.
    /// Subscribe in OnEnable, unsubscribe in OnDisable.
    /// Never add, remove, or rename existing events — only append new ones.
    /// </summary>
    public static class GameEvents
    {
        // ─────────────────────────────────────────────────────────────
        // SCANNER
        // ─────────────────────────────────────────────────────────────

        public static event Action<Scanner.ScanResult> OnScanComplete;
        public static event Action<Scanner.IScannable> OnScanBegin;
        public static event Action OnScanInterrupted;

        public static void RaiseScanComplete(Scanner.ScanResult result) => OnScanComplete?.Invoke(result);
        public static void RaiseScanBegin(Scanner.IScannable target) => OnScanBegin?.Invoke(target);
        public static void RaiseScanInterrupted() => OnScanInterrupted?.Invoke();

        // ─────────────────────────────────────────────────────────────
        // ADAPTATIONS
        // ─────────────────────────────────────────────────────────────

        public static event Action<Adaptations.AdaptationSO> OnAdaptationUnlocked;

        public static void RaiseAdaptationUnlocked(Adaptations.AdaptationSO adaptation)
            => OnAdaptationUnlocked?.Invoke(adaptation);

        // ─────────────────────────────────────────────────────────────
        // WORLD STATE / BIOME HEALTH
        // ─────────────────────────────────────────────────────────────

        /// <summary>Raised by BiomeHealthManager AFTER health changes. biomeID + new health value (0–100).</summary>
        public static event Action<string, float> OnBiomeHealthChanged;

        /// <summary>Raised by environmental sources (stalactites, hazards) to REQUEST a health change.
        /// BiomeHealthManager subscribes and applies. Never call BiomeHealthManager directly.</summary>
        public static event Action<string, float> OnBiomeHealthDelta;

        public static void RaiseBiomeHealthChanged(string biomeID, float health)
            => OnBiomeHealthChanged?.Invoke(biomeID, health);

        public static void RaiseBiomeHealthDelta(string biomeID, float delta)
            => OnBiomeHealthDelta?.Invoke(biomeID, delta);

        // ─────────────────────────────────────────────────────────────
        // CREW
        // ─────────────────────────────────────────────────────────────

        public static event Action<string, float> OnCrewMoraleChanged;

        public static void RaiseCrewMoraleChanged(string crewID, float morale)
            => OnCrewMoraleChanged?.Invoke(crewID, morale);

        // ─────────────────────────────────────────────────────────────
        // CREATURE / ENCOUNTER
        // ─────────────────────────────────────────────────────────────

        /// <summary>Raised by FoodMarker.Start(). position = world-space landing point.</summary>
        public static event Action<Vector2> OnFoodPlaced;

        /// <summary>Raised by PlayerController. 0 = Cyan, 1 = Amber.</summary>
        public static event Action<int> OnPaletteInput;

        public static void RaiseFoodPlaced(Vector2 position) => OnFoodPlaced?.Invoke(position);
        public static void RaisePaletteInput(int index) => OnPaletteInput?.Invoke(index);

        // ─────────────────────────────────────────────────────────────
        // PLAYER STATE
        // ─────────────────────────────────────────────────────────────

        /// <summary>true = entering slide, false = exiting slide.</summary>
        public static event Action<bool> OnSlideStateChanged;

        /// <summary>true = entering swim, false = exiting swim.</summary>
        public static event Action<bool> OnSwimStateChanged;

        public static void RaiseSlideStateChanged(bool isSliding) => OnSlideStateChanged?.Invoke(isSliding);
        public static void RaiseSwimStateChanged(bool isSwimming) => OnSwimStateChanged?.Invoke(isSwimming);

        // ─────────────────────────────────────────────────────────────
        // WATER / SWIMMING
        // ─────────────────────────────────────────────────────────────

        /// <summary>Raised by WaterVolume on trigger enter. float = world-space surface Y.</summary>
        public static event Action<float> OnWaterEntered;

        /// <summary>Raised by WaterVolume on trigger exit.</summary>
        public static event Action OnWaterExited;

        /// <summary>Raised by WaterTriggerHandler on splash. position = world-space splash point.</summary>
        public static event Action<Vector2, float, float> OnWaterSplash;

        /// <summary>Raised by WaterSubmersionController. bool = true when submerged, false when surfaced.</summary>
        public static event Action<bool> OnPlayerSubmerged;

        public static void RaiseWaterEntered(float surfaceY) => OnWaterEntered?.Invoke(surfaceY);
        public static void RaiseWaterExited() => OnWaterExited?.Invoke();
        public static void RaiseWaterSplash(Vector2 center, float radius, float force) => OnWaterSplash?.Invoke(center, radius, force);
        public static void RaisePlayerSubmerged(bool isSubmerged) => OnPlayerSubmerged?.Invoke(isSubmerged);

        // ─────────────────────────────────────────────────────────────
        // OXYGEN
        // ─────────────────────────────────────────────────────────────

        /// <summary>Raised by OxygenManager every Update while swimming. normalizedOxygen = 0–1 (1 = full).</summary>
        public static event Action<float> OnOxygenChanged;

        /// <summary>Raised once per dive when oxygen drops to CriticalThreshold.</summary>
        public static event Action OnOxygenCritical;

        /// <summary>Raised once when oxygen reaches 0.</summary>
        public static event Action OnOxygenDepleted;

        public static void RaiseOxygenChanged(float normalizedOxygen) => OnOxygenChanged?.Invoke(normalizedOxygen);
        public static void RaiseOxygenCritical() => OnOxygenCritical?.Invoke();
        public static void RaiseOxygenDepleted() => OnOxygenDepleted?.Invoke();

        // ─────────────────────────────────────────────────────────────
        // ENVIRONMENTAL HAZARDS
        // ─────────────────────────────────────────────────────────────

        /// <summary>Raised by StalactiteHazard when it begins falling. position = world-space origin.</summary>
        public static event Action<Vector2> OnStalactiteFall;

        /// <summary>Raised by StalactiteHazard when it lands. position = world-space landing point.</summary>
        public static event Action<Vector2> OnStalactiteLanded;

        public static void RaiseStalactiteFall(Vector2 position) => OnStalactiteFall?.Invoke(position);
        public static void RaiseStalactiteLanded(Vector2 position) => OnStalactiteLanded?.Invoke(position);

        // ─────────────────────────────────────────────────────────────
        // TRAVERSAL
        // ─────────────────────────────────────────────────────────────

        /// <summary>Raised by JumpPad when it launches the player. position = pad world-space position.</summary>
        public static event Action<Vector2> OnJumpPadLaunched;

        public static void RaiseJumpPadLaunched(Vector2 position) => OnJumpPadLaunched?.Invoke(position);

        // ─────────────────────────────────────────────────────────────
        // COMBAT
        // ─────────────────────────────────────────────────────────────

        /// <summary>Raised by PlayerController when the throw input is pressed (T key).</summary>
        public static event Action OnThrowInput;

        public static void RaiseThrowInput() => OnThrowInput?.Invoke();

        // ─────────────────────────────────────────────────────────────
        // RESOURCES
        // ─────────────────────────────────────────────────────────────

        /// <summary>Raised by ResourceNode when a resource is collected.
        /// string = ResourceNodeType name, int = amount collected.</summary>
        public static event Action<string, int> OnResourceCollected;

        public static void RaiseResourceCollected(string resourceType, int amount)
            => OnResourceCollected?.Invoke(resourceType, amount);
    }
}