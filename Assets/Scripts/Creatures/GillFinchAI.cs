// ─────────────────────────────────────────────────────────────────────────────
// GillFinchAI.cs
// Path: Assets/Scripts/Creatures/GillFinchAI.cs
// Terra's Heart — Gill-Finch behaviour for the Brineglow Pool (Room 3).
//
// Attach to: the Gill-Finch GameObject in the pool
// Requires: Collider2D on the Scannable physics layer
//
// Behaviour:
//   The Gill-Finch cycles between submerged and surfaced states.
//   Submerged: collider disabled — scanner raycast cannot hit it.
//   Surfaced:  collider enabled  — scanner can lock on for the 2-second window.
//
// Approach condition: player must be at the pool edge during the surface window.
// The short window (2s) and the need to be pre-positioned create the timing
// challenge described in the Phase A design spec.
//
// Vertical movement:
//   The GameObject moves between _submergedY and _surfacedY.
//   Place the Gill-Finch at _surfacedY in the scene. The script handles
//   the cycle from the start. Y positions are world-space.
//
// IScannable notes:
//   IsAlive  — always true. Gill-Finch cannot be killed in Step 5.
//   GetData  — assign GillFinchData.asset in the Inspector (future SO).
//              For Step 5 graybox, a CreatureDataSO with BiomeID BrineglowDescent
//              and AliveTier Rare is sufficient.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Scanner;

namespace TerrasHeart.Creatures
{
    [RequireComponent(typeof(Collider2D))]
    public class GillFinchAI : MonoBehaviour, IScannable
    {
        // ─── IScannable data ──────────────────────────────────────────────────
        [Header("Scan Data")]
        [Tooltip("Assign GillFinchData.asset — BiomeID must be 'BrineglowDescent'.")]
        [SerializeField] private CreatureDataSO _data;

        // ─── Cycle timing ─────────────────────────────────────────────────────
        [Header("Cycle")]
        [Tooltip("How many seconds the Gill-Finch stays submerged per cycle.")]
        [SerializeField] private float _submergedDuration = 6f;

        [Tooltip("How many seconds the Gill-Finch stays surfaced per cycle.")]
        [SerializeField] private float _surfacedDuration = 2f;

        [Tooltip("Speed of vertical movement between surface and submerge positions.")]
        [SerializeField] private float _riseSpeed = 3f;

        // ─── Positions ────────────────────────────────────────────────────────
        [Header("Positions")]
        [Tooltip("World-space Y when fully surfaced. Set to the pool waterline.")]
        [SerializeField] private float _surfacedY;

        [Tooltip("World-space Y when fully submerged. Set below the pool geometry.")]
        [SerializeField] private float _submergedY;

        // ─── Runtime ──────────────────────────────────────────────────────────

        private enum State { Submerged, Rising, Surfaced, Submerging }
        private State _state = State.Submerged;

        private float     _cycleTimer;
        private Collider2D _collider;
        private bool       _isBeingScanned;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            // Begin in submerged state
            Vector3 pos = transform.position;
            pos.y               = _submergedY;
            transform.position  = pos;
            _collider.enabled   = false;
            _cycleTimer         = 0f;
        }

        private void Update()
        {
            _cycleTimer += Time.deltaTime;

            switch (_state)
            {
                case State.Submerged:   UpdateSubmerged();   break;
                case State.Rising:      UpdateRising();      break;
                case State.Surfaced:    UpdateSurfaced();    break;
                case State.Submerging:  UpdateSubmerging();  break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // State Updates
        // ─────────────────────────────────────────────────────────────────────

        private void UpdateSubmerged()
        {
            if (_cycleTimer >= _submergedDuration)
            {
                _cycleTimer = 0f;
                _state      = State.Rising;
                Debug.Log("[GillFinchAI] Rising to surface.");
            }
        }

        private void UpdateRising()
        {
            Vector3 pos = transform.position;
            pos.y              = Mathf.MoveTowards(pos.y, _surfacedY, _riseSpeed * Time.deltaTime);
            transform.position = pos;

            if (Mathf.Abs(pos.y - _surfacedY) < 0.05f)
            {
                _collider.enabled = true;   // Scannable window opens
                _cycleTimer       = 0f;
                _state            = State.Surfaced;
                Debug.Log("[GillFinchAI] Surfaced. Scan window open.");
            }
        }

        private void UpdateSurfaced()
        {
            if (_cycleTimer >= _surfacedDuration)
            {
                _collider.enabled = false;  // Scannable window closes
                _cycleTimer       = 0f;
                _state            = State.Submerging;
                Debug.Log("[GillFinchAI] Submerging. Scan window closed.");
            }
        }

        private void UpdateSubmerging()
        {
            Vector3 pos = transform.position;
            pos.y              = Mathf.MoveTowards(pos.y, _submergedY, _riseSpeed * Time.deltaTime);
            transform.position = pos;

            if (Mathf.Abs(pos.y - _submergedY) < 0.05f)
            {
                _cycleTimer = 0f;
                _state      = State.Submerged;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // IScannable
        // ─────────────────────────────────────────────────────────────────────

        public bool IsAlive => true;  // Cannot be killed in Step 5

        public CreatureDataSO GetData() => _data;

        public void OnScanBegin()
        {
            _isBeingScanned = true;
        }

        public void OnScanComplete()
        {
            _isBeingScanned = false;
            // Future: post-scan surface animation, then standard submerge cycle continues
        }

        public void OnScanInterrupted()
        {
            _isBeingScanned = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            // Surface line
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.5f, _surfacedY, 0f),
                new Vector3(transform.position.x + 0.5f, _surfacedY, 0f));

            // Submerged line
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.5f, _submergedY, 0f),
                new Vector3(transform.position.x + 0.5f, _submergedY, 0f));
        }
    }
}
