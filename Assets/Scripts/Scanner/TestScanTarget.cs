// ─────────────────────────────────────────────────────────────────────────────
// TestScanTarget.cs
// Path: Assets/Scripts/Scanner/TestScanTarget.cs
// Terra's Heart — Minimal IScannable implementation for testing the scanner loop.
//
// Attach to: Any GameObject in PrologueMeridianOcean that has:
//   • A Collider2D (any type — BoxCollider2D is fine for testing)
//   • Its Layer set to "Scannable" (the layer defined in ScannerConfigSO)
//
// Wiring this to a crew member or a prop on the ship deck is enough to prove
// the full scan loop: input → raycast → lock → timer → ScanResult → Journal entry.
//
// Replace with creature-specific IScannable implementations during vertical slice.
// This class is a scaffold — it does not carry production data.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace TerrasHeart.Scanner
{
    public class TestScanTarget : MonoBehaviour, IScannable
    {
        [Header("Test Data")]
        [Tooltip("Assign a CreatureDataSO from Assets/Data/ScriptableObjects/Creatures/. " +
                 "For prologue testing, create a dummy 'TestSpecimen' SO with any placeholder values.")]
        [SerializeField] private CreatureDataSO _data;

        [Tooltip("Toggle this in the Inspector during Play mode to test alive vs dead tier logic. " +
                 "Alive = Rare tier. Dead = Common tier. Watch the console output change.")]
        [SerializeField] private bool _isAlive = true;

        // ─── IScannable Implementation ────────────────────────────────────────

        public bool IsAlive => _isAlive;

        public CreatureDataSO GetData() => _data;

        public void OnScanBegin()
        {
            Debug.Log($"[TestScanTarget] Scan begun on '{gameObject.name}'. Hold to complete.");
            // Future: trigger highlight shader or awareness animation here
        }

        public void OnScanComplete()
        {
            Debug.Log($"[TestScanTarget] Scan complete on '{gameObject.name}'.");
            // Future: trigger collected-state visual change, disable re-scan if desired
        }

        public void OnScanInterrupted()
        {
            Debug.Log($"[TestScanTarget] Scan interrupted on '{gameObject.name}'.");
            // Future: revert highlight shader
        }

#if UNITY_EDITOR
        // ─── Editor Gizmo ─────────────────────────────────────────────────────
        // Draws a cyan diamond in the Scene view so TestScanTargets are easy to find.
        private void OnDrawGizmos()
        {
            Gizmos.color = _isAlive
                ? new Color(0f, 1f, 0.83f, 0.6f)   // Neon cyan — alive
                : new Color(0.8f, 0.5f, 0f, 0.6f);  // Amber — dead

            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.4f);
        }
#endif
    }
}
