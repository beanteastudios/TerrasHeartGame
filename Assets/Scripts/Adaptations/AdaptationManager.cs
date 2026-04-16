// ─────────────────────────────────────────────────────────────────────────────
// AdaptationManager.cs
// Path: Assets/Scripts/Adaptations/AdaptationManager.cs
// Terra's Heart — Manages Dr. Maria's unlocked adaptations and stat bonuses.
//
// Attach to: DrMaria
//
// Responsibilities:
//   - Receives craft requests from CraftingController
//   - Checks SpecimenInventory for blueprint requirements
//   - Consumes specimens and adds the adaptation to the unlocked list
//   - Exposes GetJumpBonus() / GetMoveSpeedBonus() for PlayerController to read
//   - Exposes HasEffect() for bool-type adaptations (e.g. BioluminescentCamo)
//   - Fires GameEvents.OnAdaptationUnlocked
//
// Step 5 addition: HasEffect(AdaptationEffectType) — used by GlowMantleAI
// to check whether Skin / BioluminescentCamo is active before entering aggro.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.Adaptations
{
    public class AdaptationManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Assign the SpecimenInventory component on DrMaria.")]
        [SerializeField] private SpecimenInventory _inventory;

        // All adaptations Dr. Maria has unlocked this session.
        private readonly List<AdaptationSO> _unlocked = new List<AdaptationSO>();

        // ─────────────────────────────────────────────────────────────────────
        // Crafting
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to craft the adaptation defined by the given blueprint.
        /// Checks inventory requirements, consumes specimens if met, unlocks adaptation.
        /// Returns true if crafting succeeded.
        /// </summary>
        public bool TryCraft(BlueprintSO blueprint)
        {
            if (blueprint == null)
            {
                Debug.LogWarning("[AdaptationManager] TryCraft called with null blueprint.");
                return false;
            }

            if (blueprint.YieldsAdaptation == null)
            {
                Debug.LogWarning($"[AdaptationManager] Blueprint '{blueprint.name}' has no " +
                                 "YieldsAdaptation assigned.");
                return false;
            }

            if (IsUnlocked(blueprint.YieldsAdaptation))
            {
                Debug.Log($"[AdaptationManager] '{blueprint.YieldsAdaptation.DisplayName}' " +
                          "is already unlocked.");
                return false;
            }

            if (!_inventory.HasSpecimens(
                    blueprint.RequiredCreature,
                    blueprint.MinimumTier,
                    blueprint.RequiredCount))
            {
                Debug.Log($"[AdaptationManager] Cannot craft '{blueprint.YieldsAdaptation.DisplayName}'. " +
                          $"Need {blueprint.RequiredCount}x {blueprint.RequiredCreature.SpeciesName} " +
                          $"(tier >= {blueprint.MinimumTier}).");
                return false;
            }

            _inventory.ConsumeSpecimens(
                blueprint.RequiredCreature,
                blueprint.MinimumTier,
                blueprint.RequiredCount);

            _unlocked.Add(blueprint.YieldsAdaptation);
            GameEvents.RaiseAdaptationUnlocked(blueprint.YieldsAdaptation);

            Debug.Log($"[AdaptationManager] ADAPTATION UNLOCKED - " +
                      $"{blueprint.YieldsAdaptation.DisplayName} " +
                      $"[{blueprint.YieldsAdaptation.Slot}]");

            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Stat Bonus Queries — called by PlayerController each frame/jump
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the total jump force bonus from all unlocked Legs adaptations.
        /// Called by PlayerController at the moment of jump.
        /// </summary>
        public float GetJumpBonus()
        {
            return SumEffect(AdaptationEffectType.JumpForceBonus);
        }

        /// <summary>
        /// Returns the total move speed bonus from all unlocked adaptations.
        /// Called by PlayerController in FixedUpdate.
        /// </summary>
        public float GetMoveSpeedBonus()
        {
            return SumEffect(AdaptationEffectType.MoveSpeedBonus);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Bool Effect Query — Step 5 addition
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if Dr. Maria has any unlocked adaptation with the given effect type.
        /// Used for boolean adaptations such as BioluminescentCamo, ToxicGasImmunity, DarkVision.
        /// GlowMantleAI calls this before entering aggro state:
        ///   if (HasEffect(AdaptationEffectType.BioluminescentCamo)) skip aggro;
        /// </summary>
        public bool HasEffect(AdaptationEffectType effectType)
        {
            foreach (AdaptationSO adaptation in _unlocked)
            {
                if (adaptation.EffectType == effectType)
                    return true;
            }
            return false;
        }

        /// <summary>Returns true if the given adaptation is already unlocked.</summary>
        public bool IsUnlocked(AdaptationSO adaptation) => _unlocked.Contains(adaptation);

        /// <summary>Returns a read-only snapshot of all unlocked adaptations.</summary>
        public IReadOnlyList<AdaptationSO> GetUnlocked() => _unlocked;

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers
        // ─────────────────────────────────────────────────────────────────────

        private float SumEffect(AdaptationEffectType effectType)
        {
            float total = 0f;
            foreach (AdaptationSO adaptation in _unlocked)
            {
                if (adaptation.EffectType == effectType)
                    total += adaptation.EffectValue;
            }
            return total;
        }
    }
}
