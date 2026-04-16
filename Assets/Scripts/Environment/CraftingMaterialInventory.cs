// ─────────────────────────────────────────────────────────────────────────────
// CraftingMaterialInventory.cs
// Path: Assets/Scripts/Environment/CraftingMaterialInventory.cs
// Terra's Heart — Tracks Dr. Maria's crafting material counts.
//
// Attach to: DrMaria (alongside SpecimenInventory)
//
// Listens to GameEvents.OnResourceCollected and increments the count
// for the relevant ResourceNodeType. Counts persist across scene loads
// via PersistentEntity on DrMaria.
//
// Step 5 scope: count tracking and Debug logging only.
// No field kit slot limit is enforced in graybox — that is a production-phase
// UI and inventory management task. The inventory pressure design is
// communicated through player awareness, not a hard cap in Step 5.
//
// Queried by: future Meridian repair system (production phase).
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.Environment
{
    public class CraftingMaterialInventory : MonoBehaviour
    {
        // Count per ResourceNodeType
        private readonly Dictionary<ResourceNodeType, int> _counts
            = new Dictionary<ResourceNodeType, int>();

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameEvents.OnResourceCollected += HandleResourceCollected;
        }

        private void OnDisable()
        {
            GameEvents.OnResourceCollected -= HandleResourceCollected;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Event Handler
        // ─────────────────────────────────────────────────────────────────────

        private void HandleResourceCollected(ResourceNodeType nodeType, int amount)
        {
            if (!_counts.ContainsKey(nodeType))
                _counts[nodeType] = 0;

            _counts[nodeType] += amount;

            Debug.Log($"[CraftingMaterials] +{amount} {nodeType} — " +
                      $"total: {_counts[nodeType]}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API — queried by future Meridian repair system
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the current count of the given material type.
        /// </summary>
        public int GetCount(ResourceNodeType nodeType)
        {
            return _counts.TryGetValue(nodeType, out int count) ? count : 0;
        }

        /// <summary>
        /// Returns true if the inventory contains at least the required amount.
        /// </summary>
        public bool Has(ResourceNodeType nodeType, int requiredAmount)
        {
            return GetCount(nodeType) >= requiredAmount;
        }

        /// <summary>
        /// Consumes the given amount of the material type.
        /// Returns false if insufficient — does not consume partial amounts.
        /// </summary>
        public bool Consume(ResourceNodeType nodeType, int amount)
        {
            if (!Has(nodeType, amount))
            {
                Debug.LogWarning($"[CraftingMaterials] Cannot consume {amount}x {nodeType} " +
                                 $"— only {GetCount(nodeType)} available.");
                return false;
            }

            _counts[nodeType] -= amount;
            Debug.Log($"[CraftingMaterials] Consumed {amount}x {nodeType} — " +
                      $"remaining: {_counts[nodeType]}");
            return true;
        }

        /// <summary>
        /// Returns a summary string of all current material counts for debug display.
        /// </summary>
        public string GetInventorySummary()
        {
            if (_counts.Count == 0) return "No crafting materials.";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (KeyValuePair<ResourceNodeType, int> pair in _counts)
                sb.AppendLine($"  {pair.Key}: {pair.Value}");

            return sb.ToString();
        }
    }
}
