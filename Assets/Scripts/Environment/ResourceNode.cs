// ─────────────────────────────────────────────────────────────────────────────
// ResourceNode.cs
// Path: Assets/Scripts/Environment/ResourceNode.cs
// Terra's Heart — Physical resource pickup in the world.
//
// Attach to: every resource node GameObject in BrineglowDescent.
// Requires: one Collider2D set as Trigger for proximity detection.
//           (A second non-trigger Collider2D can be added for visual collision
//            if the node has physical geometry — not required for graybox.)
//
// PICKUP FLOW:
//   1. Player enters trigger radius.
//   2. Player presses E key.
//   3. Node fires GameEvents.RaiseResourceCollected(nodeType, 1).
//   4. Node deactivates itself (one-time pickup — Step 5 nodes do not respawn).
//
// OPTIONAL SCANNER INTEGRATION:
//   Set _isScanTarget = true on Synthetic nodes and Thari-origin nodes
//   that should log a Research Journal entry when scanned.
//   Assign a CreatureDataSO with the artefact display name and field note.
//   The scan pipeline handles journal logging automatically.
//   Physical pickup (E key) is independent of scanning — the player can
//   pick it up without scanning it, but the journal entry won't be logged.
//
// BIOLOGICAL NODE DEPLETION:
//   Set _nodeType = Biological and _depletesAtLowHealth = true.
//   The node subscribes to OnBiomeHealthChanged and disables itself if the
//   biome health for its biomeID drops below _depletionHealthThreshold.
//   This implements the design rule: biological nodes can be unavailable
//   at very low biome health — an acceptable ecological consequence.
//   Geological, Synthetic, and Thari-origin nodes are never depleted.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;
using TerrasHeart.Scanner;
using TerrasHeart.Events;

namespace TerrasHeart.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public class ResourceNode : MonoBehaviour, IScannable
    {
        // ─── Node identity ────────────────────────────────────────────────────
        [Header("Node")]
        [SerializeField] private ResourceNodeType _nodeType = ResourceNodeType.Biological;

        [Tooltip("Display name shown in Debug logs. Used as node identifier.")]
        [SerializeField] private string _nodeName = "Resource Node";

        // ─── Biological depletion ─────────────────────────────────────────────
        [Header("Biological Depletion")]
        [Tooltip("Only relevant when NodeType = Biological. " +
                 "If true, this node disables itself when biome health drops below threshold.")]
        [SerializeField] private bool _depletesAtLowHealth = false;

        [Tooltip("BiomeID to listen for. Must match BiomeHealthManager BiomeID exactly: " +
                 "'BrineglowDescent'.")]
        [SerializeField] private string _biomeID = "BrineglowDescent";

        [Tooltip("Health threshold (0–100) below which this Biological node depletes.")]
        [SerializeField] private float _depletionHealthThreshold = 20f;

        // ─── Scanner integration (optional) ───────────────────────────────────
        [Header("Scanner Integration (Optional)")]
        [Tooltip("If true, this node can be scanned to log a Research Journal entry. " +
                 "Use for Synthetic (Poseidon artefacts) and Thari-origin nodes. " +
                 "Requires a Collider2D on the Scannable physics layer.")]
        [SerializeField] private bool _isScanTarget = false;

        [Tooltip("Required if IsScanTarget is true. " +
                 "Assign a CreatureDataSO with the artefact name and Dr. Maria field note. " +
                 "IsAlive on the scan result will always be true for static nodes.")]
        [SerializeField] private CreatureDataSO _scanData;

        // ─── Runtime ──────────────────────────────────────────────────────────

        private bool _playerInRange;
        private bool _collected;
        private bool _depleted;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Confirm the Collider2D is set as a trigger
            Collider2D col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                Debug.LogWarning($"[ResourceNode] '{_nodeName}': Collider2D must be set as " +
                                 "Trigger for proximity detection. Setting automatically.");
                col.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            if (_nodeType == ResourceNodeType.Biological && _depletesAtLowHealth)
                GameEvents.OnBiomeHealthChanged += HandleBiomeHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnBiomeHealthChanged -= HandleBiomeHealthChanged;
        }

        private void Update()
        {
            if (!_playerInRange || _collected || _depleted) return;

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.eKey.wasPressedThisFrame)
                Collect();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Proximity Detection
        // ─────────────────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                _playerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                _playerInRange = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Collection
        // ─────────────────────────────────────────────────────────────────────

        private void Collect()
        {
            _collected = true;

            GameEvents.RaiseResourceCollected(_nodeType, 1);

            Debug.Log($"[ResourceNode] Collected: {_nodeName} ({_nodeType})");

            // One-time pickup — deactivate the GameObject
            gameObject.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Biological Depletion
        // ─────────────────────────────────────────────────────────────────────

        private void HandleBiomeHealthChanged(string biomeID, float newHealth)
        {
            if (biomeID != _biomeID) return;
            if (_collected) return;

            if (newHealth <= _depletionHealthThreshold && !_depleted)
            {
                _depleted = true;
                Debug.Log($"[ResourceNode] '{_nodeName}' depleted — " +
                          $"biome health {newHealth:F0}% below threshold {_depletionHealthThreshold:F0}%.");

                // Future: swap to depleted visual state (colour shift, wilt animation)
                // For graybox: just make it non-interactable (keep visible but greyed out)
                // gameObject.SetActive(false) would hide it entirely — not ideal for
                // ecological storytelling. Production pass will handle visual states.
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // IScannable — only active when _isScanTarget = true
        // ─────────────────────────────────────────────────────────────────────

        // Static nodes are always "alive" — IsAlive drives scan tier in the pipeline.
        // A Poseidon pipe fragment scanned alive yields the tier set in _scanData.AliveTier.
        public bool IsAlive => true;

        public CreatureDataSO GetData()
        {
            if (!_isScanTarget)
            {
                Debug.LogWarning($"[ResourceNode] '{_nodeName}' was scanned but " +
                                 "_isScanTarget is false. Assign _scanData and enable the flag.");
                return null;
            }
            return _scanData;
        }

        public void OnScanBegin()      { }
        public void OnScanComplete()   { Debug.Log($"[ResourceNode] '{_nodeName}' scanned — journal entry logged."); }
        public void OnScanInterrupted(){ }

        // ─────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            // Colour-coded by node type for easy identification in Scene view
            switch (_nodeType)
            {
                case ResourceNodeType.Biological:  Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f); break;
                case ResourceNodeType.Geological:  Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.4f); break;
                case ResourceNodeType.Synthetic:   Gizmos.color = new Color(1f, 0.6f, 0f, 0.4f); break;
                case ResourceNodeType.ThariOrigin: Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f); break;
            }
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
    }
}
