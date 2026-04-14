// ─────────────────────────────────────────────────────────────────────────────
// CraftingController.cs
// Path: Assets/Scripts/Adaptations/CraftingController.cs
// Terra's Heart — Handles crafting input and passes blueprint requests to
// AdaptationManager.
//
// Attach to: DrMaria
//
// Input style: Keyboard.current polling — matches PlayerController and
// ScannerController. Craft key: C.
//
// MVP behaviour: pressing C attempts all blueprints in _availableBlueprints
// in order, stopping at the first successful craft. This is enough to prove
// the reward pipeline. Future: replace with a crafting UI that lets the player
// choose which blueprint to craft at the Meridian workbench.
//
// Note: In production, blueprints will be gated — they appear in
// _availableBlueprints only after the required creature has been scanned
// and Callum has been assigned to the crafting task. For MVP this is manual.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;

namespace TerrasHeart.Adaptations
{
    public class CraftingController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Assign the AdaptationManager component on DrMaria.")]
        [SerializeField] private AdaptationManager _adaptationManager;

        [Header("Available Blueprints")]
        [Tooltip("Blueprints Dr. Maria currently has access to. " +
                 "For MVP: drag your test BlueprintSO here. " +
                 "Future: populated dynamically by Callum's research assignments.")]
        [SerializeField] private BlueprintSO[] _availableBlueprints;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.cKey.wasPressedThisFrame)
                TryCraftNext();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Crafting
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts each available blueprint in order and stops at the first
        /// successful craft. Logs the result to the console for MVP proof.
        /// </summary>
        private void TryCraftNext()
        {
            if (_adaptationManager == null)
            {
                Debug.LogWarning("[CraftingController] AdaptationManager reference is not assigned.");
                return;
            }

            if (_availableBlueprints == null || _availableBlueprints.Length == 0)
            {
                Debug.Log("[CraftingController] No blueprints available to craft.");
                return;
            }

            foreach (BlueprintSO blueprint in _availableBlueprints)
            {
                if (blueprint == null) continue;

                bool success = _adaptationManager.TryCraft(blueprint);
                if (success) return; // Stop after first successful craft
            }

            Debug.Log("[CraftingController] No blueprints could be crafted. " +
                      "Check specimen inventory and blueprint requirements.");
        }
    }
}
