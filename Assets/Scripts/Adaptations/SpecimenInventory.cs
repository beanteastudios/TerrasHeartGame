// ─────────────────────────────────────────────────────────────────────────────
// SpecimenInventory.cs
// Path: Assets/Scripts/Adaptations/SpecimenInventory.cs
// Terra's Heart — Dr. Maria's specimen collection.
//
// Attach to: DrMaria
//
// Listens to GameEvents.OnScanComplete and stores one SpecimenStack per
// (species, tier) pair. Stacks accumulate count as the player scans more.
//
// Queried by AdaptationManager.TryCraft() to check and consume requirements.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using TerrasHeart.Events;
using TerrasHeart.Scanner;

namespace TerrasHeart.Adaptations
{
    public class SpecimenInventory : MonoBehaviour
    {
        // All specimen stacks collected this session.
        private readonly List<SpecimenStack> _stacks = new List<SpecimenStack>();

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameEvents.OnScanComplete += HandleScanComplete;
        }

        private void OnDisable()
        {
            GameEvents.OnScanComplete -= HandleScanComplete;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Event Handler
        // ─────────────────────────────────────────────────────────────────────

        private void HandleScanComplete(ScanResult result)
        {
            if (result?.SourceData == null) return;

            SpecimenStack stack = GetOrCreateStack(result.SourceData, result.Tier);
            stack.Add(1);

            Debug.Log($"[Inventory] +1 {result.SourceData.SpeciesName} ({result.Tier}) " +
                      $"— total: {stack.Count}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API — queried by AdaptationManager
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the inventory contains at least <paramref name="requiredCount"/>
        /// specimens of the given creature at or above <paramref name="minimumTier"/>.
        /// </summary>
        public bool HasSpecimens(CreatureDataSO creature, SpecimenTier minimumTier, int requiredCount)
        {
            int total = CountAvailable(creature, minimumTier);
            return total >= requiredCount;
        }

        /// <summary>
        /// Consumes <paramref name="amount"/> specimens of the given creature at or above
        /// <paramref name="minimumTier"/>, starting from the highest tier stacks first
        /// (to preserve lower-tier specimens for other blueprints where possible).
        /// Returns true if the full amount was consumed. Returns false if insufficient.
        /// </summary>
        public bool ConsumeSpecimens(CreatureDataSO creature, SpecimenTier minimumTier, int amount)
        {
            if (!HasSpecimens(creature, minimumTier, amount))
            {
                Debug.LogWarning($"[Inventory] Cannot consume {amount}x {creature.SpeciesName} " +
                                 $"(tier ≥ {minimumTier}) — insufficient stock.");
                return false;
            }

            // Consume from highest tier first to preserve lower-tier specimens
            for (int tier = (int)SpecimenTier.AurTouched; tier >= (int)minimumTier && amount > 0; tier--)
            {
                SpecimenStack stack = FindStack(creature, (SpecimenTier)tier);
                if (stack == null || stack.Count == 0) continue;

                int toConsume = Mathf.Min(stack.Count, amount);
                stack.TryConsume(toConsume);
                amount -= toConsume;
            }

            return true;
        }

        /// <summary>Returns a read-only snapshot of all current stacks for UI display.</summary>
        public IReadOnlyList<SpecimenStack> GetAllStacks() => _stacks;

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers
        // ─────────────────────────────────────────────────────────────────────

        private SpecimenStack GetOrCreateStack(CreatureDataSO creature, SpecimenTier tier)
        {
            SpecimenStack existing = FindStack(creature, tier);
            if (existing != null) return existing;

            SpecimenStack newStack = new SpecimenStack(creature, tier);
            _stacks.Add(newStack);
            return newStack;
        }

        private SpecimenStack FindStack(CreatureDataSO creature, SpecimenTier tier)
        {
            return _stacks.Find(s => s.Creature == creature && s.Tier == tier);
        }

        private int CountAvailable(CreatureDataSO creature, SpecimenTier minimumTier)
        {
            int total = 0;
            foreach (SpecimenStack stack in _stacks)
            {
                if (stack.Creature == creature && stack.Tier >= minimumTier)
                    total += stack.Count;
            }
            return total;
        }
    }
}
