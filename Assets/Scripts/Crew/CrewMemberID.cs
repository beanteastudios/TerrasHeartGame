// ─────────────────────────────────────────────────────────────────────────────
// CrewMemberID.cs
// Path: Assets/Scripts/Crew/CrewMemberID.cs
// Terra's Heart — Unique identifier for each crew member.
// Used as the key in CrewManager's assignment dictionary.
// ─────────────────────────────────────────────────────────────────────────────

namespace TerrasHeart.Crew
{
    public enum CrewMemberID
    {
        Leif,    // First mate — base capacity, structural repairs
        Yuki,    // Data analyst — research speed, Thari translation
        Callum,  // Engineer — tool quality, new tool designs
        Amara,   // Medic — healing, morale management
        Shoresh  // Cook/observer — morale stability, passive wellbeing
    }
}
