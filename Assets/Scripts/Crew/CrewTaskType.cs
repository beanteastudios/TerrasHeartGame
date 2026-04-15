// ─────────────────────────────────────────────────────────────────────────────
// CrewTaskType.cs
// Path: Assets/Scripts/Crew/CrewTaskType.cs
// Terra's Heart — Tasks crew members can be assigned to at the Meridian.
// Each crew member has different skill bonuses per task (defined in CrewMemberDataSO).
// ─────────────────────────────────────────────────────────────────────────────

namespace TerrasHeart.Crew
{
    public enum CrewTaskType
    {
        Unassigned,   // No active task — contributes passive morale stability (Shoresh)
        Research,     // Specimen processing speed — primary: Yuki
        Crafting,     // Tool quality and adaptation crafting speed — primary: Callum
        Repair,       // Meridian structural repairs — primary: Leif
        Translation,  // Thari language progress rate — primary: Yuki
        Cooking       // Morale floor and passive morale restoration — primary: Shoresh
    }
}
