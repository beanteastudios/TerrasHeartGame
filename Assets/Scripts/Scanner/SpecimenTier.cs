// ─────────────────────────────────────────────────────────────────────────────
// SpecimenTier.cs
// Path: Assets/Scripts/Scanner/SpecimenTier.cs
// Terra's Heart — Specimen quality tier enum.
//
// Tier is determined by scan method (live vs dead) and biome health score.
// The integer value intentionally increases with rarity — safe for comparisons.
//
// Live scan:  Rare, Exceptional, or AurTouched (per CreatureDataSO.AliveTier)
// Dead scan:  Always Common (per CreatureDataSO.DeadTier)
// This gap is intentional. It is the mechanical enforcement of "science first."
// ─────────────────────────────────────────────────────────────────────────────

namespace TerrasHeart.Scanner
{
    public enum SpecimenTier
    {
        Common     = 0,
        Uncommon   = 1,
        Rare       = 2,
        Exceptional = 3,
        AurTouched  = 4   // Only available in healthy biomes with Aur-adjacent creatures
    }
}
