// ─────────────────────────────────────────────────────────────────────────────
// ResearchJournalManager.cs
// Path: Assets/Scripts/Scanner/ResearchJournalManager.cs
// Terra's Heart — The Research Journal. Dr. Maria's field record.
//
// Attach to: A dedicated "Systems" or "GameManagers" GameObject in the scene.
//            It does not need to be on DrMaria.
//
// This is the first consumer of GameEvents.OnScanComplete.
// It stores JournalEntry records in memory and logs them to the console (MVP).
//
// Future tabs driven by this manager:
//   • Species Log          — scanned creatures, field notes (Dr. Maria's voice)
//   • Evidence Dossier     — Poseidon artefacts and corporate evidence
//   • Thari Language Log   — progressive translation via Yuki's research speed
//   • Ecological Assessments — per-biome health reports
//   • Crew Reflections     — morale-threshold entries per crew member
//
// Future: connect to Research Journal UI Canvas.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.Scanner
{
    public class ResearchJournalManager : MonoBehaviour
    {
        // All entries logged this session. Exposed as read-only for future UI access.
        private readonly List<JournalEntry> _entries = new List<JournalEntry>();

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

        /// <summary>
        /// Called whenever ScannerController fires a successful scan.
        /// Creates a JournalEntry and stores it. Logs to console for MVP.
        /// </summary>
        private void HandleScanComplete(ScanResult result)
        {
            if (result == null || result.SourceData == null)
            {
                Debug.LogWarning("[ResearchJournal] Received a ScanResult with null SourceData. Entry skipped.");
                return;
            }

            CreatureDataSO data = result.SourceData;

            JournalEntry entry = new JournalEntry(
                speciesName:  data.SpeciesName,
                fieldNote:    data.FieldNote,
                biomeID:      data.BiomeID,
                tier:         result.Tier,
                wasAlive:     result.WasAlive,
                timestamp:    result.Timestamp
            );

            _entries.Add(entry);

            // ─── Console output for MVP proof ─────────────────────────────────
            bool isFirstEntry = CountEntriesFor(data.SpeciesName) == 1;

            if (isFirstEntry)
            {
                Debug.Log(
                    $"[Journal] ✦ NEW ENTRY #{_entries.Count} ─ {entry.SpeciesName}\n" +
                    $"  Biome:   {entry.BiomeID}\n" +
                    $"  Tier:    {entry.Tier}{(entry.WasAlive ? "" : " ⚠ Dead scan — ecological cost paid")}\n" +
                    $"  Note:    \"{entry.FieldNote}\""
                );
            }
            else
            {
                Debug.Log(
                    $"[Journal] Re-scan #{_entries.Count} ─ {entry.SpeciesName} | " +
                    $"Tier: {entry.Tier} | Alive: {entry.WasAlive}"
                );
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API (for future Journal UI and save system)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns all journal entries logged this session.</summary>
        public IReadOnlyList<JournalEntry> GetAllEntries() => _entries;

        /// <summary>Returns all entries for a specific species name.</summary>
        public List<JournalEntry> GetEntriesFor(string speciesName) =>
            _entries.FindAll(e => e.SpeciesName == speciesName);

        /// <summary>
        /// Returns whether a given species has been scanned at least once.
        /// Used by future UI to distinguish new vs previously-logged entries.
        /// </summary>
        public bool HasEntryFor(string speciesName) =>
            _entries.Exists(e => e.SpeciesName == speciesName);

        /// <summary>Total number of unique species names logged.</summary>
        public int UniqueSpeciesCount()
        {
            var seen = new HashSet<string>();
            foreach (var e in _entries) seen.Add(e.SpeciesName);
            return seen.Count;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers
        // ─────────────────────────────────────────────────────────────────────

        private int CountEntriesFor(string speciesName) =>
            _entries.FindAll(e => e.SpeciesName == speciesName).Count;
    }
}
