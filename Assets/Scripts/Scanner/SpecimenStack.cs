// ─────────────────────────────────────────────────────────────────────────────
// SpecimenStack.cs
// Path: Assets/Scripts/Scanner/SpecimenStack.cs
// Terra's Heart — One entry in Dr. Maria's specimen inventory.
// Groups specimens by species + tier. Count tracks how many have been collected.
//
// Not a ScriptableObject — this is runtime inventory data, not a static definition.
// Stored in SpecimenInventory._stacks.
// ─────────────────────────────────────────────────────────────────────────────

namespace TerrasHeart.Scanner
{
    /// <summary>
    /// A stack of specimens of the same species and tier in Dr. Maria's inventory.
    /// Created by SpecimenInventory when a scan completes.
    /// Consumed by AdaptationManager.TryCraft() when a blueprint is crafted.
    /// </summary>
    public class SpecimenStack
    {
        public CreatureDataSO Creature { get; }
        public SpecimenTier Tier       { get; }
        public int Count               { get; private set; }

        public SpecimenStack(CreatureDataSO creature, SpecimenTier tier)
        {
            Creature = creature;
            Tier     = tier;
            Count    = 0;
        }

        /// <summary>Adds specimens to this stack.</summary>
        public void Add(int amount = 1) => Count += amount;

        /// <summary>
        /// Attempts to consume the requested amount from this stack.
        /// Returns true and deducts if sufficient. Returns false and changes nothing if not.
        /// </summary>
        public bool TryConsume(int amount)
        {
            if (Count < amount) return false;
            Count -= amount;
            return true;
        }
    }
}
