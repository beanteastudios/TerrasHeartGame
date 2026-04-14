// ─────────────────────────────────────────────────────────────────────────────
// AdaptationEffectType.cs
// Path: Assets/Scripts/Adaptations/AdaptationEffectType.cs
// Terra's Heart — What an adaptation actually does to Dr. Maria.
// AdaptationManager queries these types to compute stat bonuses.
// Extend this enum as new adaptation mechanics are added per biome.
// ─────────────────────────────────────────────────────────────────────────────

namespace TerrasHeart.Adaptations
{
    public enum AdaptationEffectType
    {
        // ─── Legs slot ────────────────────────────────────────────────────────
        JumpForceBonus,     // Adds to base jump force in PlayerController
        MoveSpeedBonus,     // Adds to base move speed in PlayerController

        // ─── Lungs slot ──────────────────────────────────────────────────────
        ToxicGasImmunity,   // Bool — player immune to toxic gas zones
        UnderwaterBreath,   // Bool — player can breathe underwater

        // ─── Eyes slot ───────────────────────────────────────────────────────
        DarkVision,         // Bool — player sees in dark caves
        AurPerception,      // Bool — player can perceive Ancestor Thari resonance

        // ─── Skin slot ───────────────────────────────────────────────────────
        ToxinResistance,    // Damage reduction from toxin sources
        BioluminescentCamo, // Bool — camouflage in bioluminescent zones

        // ─── Arms slot ───────────────────────────────────────────────────────
        VineGrapple,        // Bool — unlocks grapple mechanic

        // ─── Core slot ───────────────────────────────────────────────────────
        AurResonance,       // Bool — unlocks Aur-powered structure interaction
        HealingPulse        // Passive heal amount per second
    }
}
