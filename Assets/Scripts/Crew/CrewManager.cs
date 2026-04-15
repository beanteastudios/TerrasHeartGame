// ─────────────────────────────────────────────────────────────────────────────
// CrewManager.cs
// Path: Assets/Scripts/Crew/CrewManager.cs
// Terra's Heart — Tracks crew task assignments and exposes skill bonuses.
//
// Attach to: GameManagers
//
// MVP behaviour: assignments are set directly in the Inspector via the
// _assignments array. In Phase 1, a Meridian assignment UI will call
// AssignCrew() and UnassignCrew() at runtime.
//
// ScannerController reads GetScanDurationMultiplier() each scan.
// Future systems read GetCraftingMultiplier(), GetRepairMultiplier() etc.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.Crew
{
    public class CrewManager : MonoBehaviour
    {
        [Header("Crew Data")]
        [Tooltip("Assign all five CrewMemberDataSO assets here. Order doesn't matter.")]
        [SerializeField] private CrewMemberDataSO[] _crewData;

        [Header("Starting Assignments")]
        [Tooltip("MVP: set assignments here for testing. " +
                 "Set Yuki to Research to prove the scan duration bonus. " +
                 "In Phase 1 this will be replaced by a Meridian assignment UI.")]
        [SerializeField] private CrewAssignment[] _startingAssignments;

        // ─── Runtime State ────────────────────────────────────────────────────

        // Key: CrewMemberID, Value: current task assignment
        private readonly Dictionary<CrewMemberID, CrewTaskType> _assignments
            = new Dictionary<CrewMemberID, CrewTaskType>();

        // Lookup from ID to SO for efficient bonus queries
        private readonly Dictionary<CrewMemberID, CrewMemberDataSO> _dataLookup
            = new Dictionary<CrewMemberID, CrewMemberDataSO>();

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildDataLookup();
            ApplyStartingAssignments();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API — Assignment Control
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Assigns a crew member to a task. Fires OnCrewMoraleChanged as a
        /// proxy event for now — a dedicated OnCrewAssignmentChanged event
        /// will be added to GameEvents in Phase 1.
        /// </summary>
        public void AssignCrew(CrewMemberID member, CrewTaskType task)
        {
            _assignments[member] = task;

            string name = GetDisplayName(member);
            Debug.Log($"[CrewManager] {name} → {task}");

            // Notify other systems (future: UI, productivity systems)
            GameEvents.RaiseCrewMoraleChanged(member.ToString(), 0f);
        }

        /// <summary>Returns the current task assignment for a crew member.</summary>
        public CrewTaskType GetAssignment(CrewMemberID member)
        {
            return _assignments.TryGetValue(member, out CrewTaskType task)
                ? task
                : CrewTaskType.Unassigned;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API — Bonus Queries (called by gameplay systems)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the scan hold duration multiplier from any crew member
        /// currently assigned to Research.
        /// Multiple crew on Research: takes the best (lowest) multiplier.
        /// If no one is on Research: returns 1.0 (no bonus).
        /// Called by ScannerController every scan.
        /// </summary>
        public float GetScanDurationMultiplier()
        {
            return GetBestMultiplierForTask(
                CrewTaskType.Research,
                so => so.ScanDurationMultiplier
            );
        }

        /// <summary>
        /// Returns the crafting speed multiplier from any crew member on Crafting.
        /// Future use — AdaptationManager will call this in Phase 1.
        /// </summary>
        public float GetCraftingMultiplier()
        {
            return GetBestMultiplierForTask(
                CrewTaskType.Crafting,
                so => so.CraftingSpeedMultiplier
            );
        }

        /// <summary>
        /// Returns the repair speed multiplier from any crew member on Repair.
        /// Future use — MeridianRepairManager will call this in Phase 1.
        /// </summary>
        public float GetRepairMultiplier()
        {
            return GetBestMultiplierForTask(
                CrewTaskType.Repair,
                so => so.RepairSpeedMultiplier
            );
        }

        /// <summary>
        /// Returns the translation speed multiplier from any crew member on Translation.
        /// Future use — ThariFactionManager will call this in Phase 1.
        /// </summary>
        public float GetTranslationMultiplier()
        {
            return GetBestMultiplierForTask(
                CrewTaskType.Translation,
                so => so.TranslationSpeedMultiplier
            );
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void BuildDataLookup()
        {
            if (_crewData == null) return;

            foreach (CrewMemberDataSO data in _crewData)
            {
                if (data == null) continue;
                _dataLookup[data.ID] = data;
            }
        }

        private void ApplyStartingAssignments()
        {
            // Default everyone to Unassigned first
            foreach (CrewMemberID id in System.Enum.GetValues(typeof(CrewMemberID)))
                _assignments[id] = CrewTaskType.Unassigned;

            // Apply Inspector-configured starting assignments
            if (_startingAssignments == null) return;

            foreach (CrewAssignment assignment in _startingAssignments)
            {
                _assignments[assignment.Member] = assignment.Task;
                Debug.Log($"[CrewManager] Starting assignment: " +
                          $"{GetDisplayName(assignment.Member)} → {assignment.Task}");
            }
        }

        /// <summary>
        /// Scans all crew assignments for the given task and returns the best
        /// (lowest) multiplier value — i.e. the most skilled crew member on that task.
        /// Returns 1.0 if no crew member is assigned to the task.
        /// </summary>
        private float GetBestMultiplierForTask(
            CrewTaskType task,
            System.Func<CrewMemberDataSO, float> getMultiplier)
        {
            float best = 1.0f;
            bool found = false;

            foreach (var kvp in _assignments)
            {
                if (kvp.Value != task) continue;
                if (!_dataLookup.TryGetValue(kvp.Key, out CrewMemberDataSO data)) continue;

                float value = getMultiplier(data);
                if (!found || value < best)
                {
                    best  = value;
                    found = true;
                }
            }

            return best;
        }

        private string GetDisplayName(CrewMemberID id)
        {
            return _dataLookup.TryGetValue(id, out CrewMemberDataSO data)
                ? data.DisplayName
                : id.ToString();
        }
    }

    // ─── Helper struct for Inspector-assignable starting assignments ──────────

    [System.Serializable]
    public struct CrewAssignment
    {
        public CrewMemberID Member;
        public CrewTaskType Task;
    }
}
