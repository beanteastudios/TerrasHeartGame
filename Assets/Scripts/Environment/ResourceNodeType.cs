// ─────────────────────────────────────────────────────────────────────────────
// ResourceNodeType.cs
// Path: Assets/Scripts/Environment/ResourceNodeType.cs
// Terra's Heart — Categories of physical resource nodes in the world.
//
// Used by ResourceNode and CraftingMaterialInventory.
// Matches the four confirmed node categories from the Level Design reference.
// ─────────────────────────────────────────────────────────────────────────────

namespace TerrasHeart.Environment
{
    public enum ResourceNodeType
    {
        Biological,    // Cave moss, creature residue — can deplete at low biome health
        Geological,    // Mineral seams, crystal formations — unaffected by biome health
        Synthetic,     // Poseidon wreckage, pipe fragments — unaffected by biome health
        ThariOrigin    // Near Thari structures — unaffected by biome health
    }
}
