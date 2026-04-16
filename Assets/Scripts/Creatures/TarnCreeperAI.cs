// ─────────────────────────────────────────────────────────────────────────────
// TarnCreeperAI.cs
// Path: Assets/Scripts/Creatures/TarnCreeperAI.cs
// Terra's Heart — Tarn Creeper behaviour. The corrupted creature in Room 6.
//
// Attach to: the Tarn Creeper GameObject
// Requires: Collider2D on the Scannable physics layer
//           Rigidbody2D for movement toward player
//
// STATE MACHINE:
//
//   Idle ──────► Aggro      Player enters _aggroRadius. Creeper moves toward player.
//   Aggro ─────► Subdue     Subdue health reaches 0 via PulseLance hits.
//   Subdue ────► Restored   Automatic transition. Corruption visually clears.
//   Restored ──► PostScan   Player fires scanner during the 4-second window.
//   Restored ──► PostScan   Window expires without scan — creature stays passive.
//
// The Restored and PostScan states are both passive. The creature never
// re-aggros after restoration. Rule 2: protect the scan window.
//
// SUBDUE HEALTH:
//   The Tarn Creeper has a separate _subdueHealth pool from any real HP system.
//   PulseLanceController calls ApplySubdueDamage(float) on this component.
//   When _currentSubdueHealth <= 0 the transition to Restored fires.
//   This is NOT lethal damage — the creature cannot die in Step 5.
//
// BIOME HEALTH RESTORATION:
//   Happens automatically via the existing scan pipeline.
//   When the player scans the Restored Tarn Creeper, ScannerController fires
//   GameEvents.RaiseScanComplete with IsAlive=true. BiomeHealthManager already
//   listens for this and applies +3 restoration when BiomeID matches.
//   TarnCreeperAI does not fire health events directly.
//
// IScannable notes:
//   IsAlive   — true in Restored and PostScan states only.
//               False in Idle and Aggro (dead scan = Common — science-first pillar).
//               Scanning a still-corrupted Tarn Creeper yields Common tier only.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Scanner;

namespace TerrasHeart.Creatures
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class TarnCreeperAI : MonoBehaviour, IScannable
    {
        // ─── IScannable data ──────────────────────────────────────────────────
        [Header("Scan Data")]
        [Tooltip("Assign TarnCreeperData.asset — BiomeID must be 'BrineglowDescent'. " +
                 "AliveTier: Rare. DeadTier: Common.")]
        [SerializeField] private CreatureDataSO _data;

        // ─── Detection & Movement ─────────────────────────────────────────────
        [Header("Aggro")]
        [SerializeField] private float _aggroRadius  = 5f;
        [SerializeField] private float _moveSpeed    = 2.5f;

        // ─── Subdue ───────────────────────────────────────────────────────────
        [Header("Subdue")]
        [Tooltip("Total subdue damage required to trigger restoration. " +
                 "Each Pulse Lance hit calls ApplySubdueDamage(). " +
                 "Set to match approximately 3–5 hits.")]
        [SerializeField] private float _maxSubdueHealth = 30f;

        // ─── Scan window ──────────────────────────────────────────────────────
        [Header("Scan Window")]
        [Tooltip("Seconds the scan window stays open after restoration. " +
                 "Phase A spec: 4 seconds.")]
        [SerializeField] private float _scanWindowDuration = 4f;

        // ─── Runtime ──────────────────────────────────────────────────────────

        public enum CreepState { Idle, Aggro, Subdue, Restored, PostScan }
        public CreepState State { get; private set; } = CreepState.Idle;

        private float      _currentSubdueHealth;
        private float      _scanWindowTimer;
        private Rigidbody2D _rb;
        private Transform   _playerTransform;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _currentSubdueHealth = _maxSubdueHealth;
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
            else
                Debug.LogWarning("[TarnCreeperAI] Player not found. Aggro disabled.");
        }

        private void Update()
        {
            switch (State)
            {
                case CreepState.Idle:     UpdateIdle();     break;
                case CreepState.Aggro:    UpdateAggro();    break;
                case CreepState.Restored: UpdateRestored(); break;
                // Subdue is a momentary transition state — handled in ApplySubdueDamage
                // PostScan is fully passive — no update needed
            }
        }

        private void FixedUpdate()
        {
            if (State == CreepState.Aggro)
                MoveTowardPlayer();
        }

        // ─────────────────────────────────────────────────────────────────────
        // State Updates
        // ─────────────────────────────────────────────────────────────────────

        private void UpdateIdle()
        {
            if (_playerTransform == null) return;

            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            if (dist <= _aggroRadius)
            {
                State = CreepState.Aggro;
                Debug.Log("[TarnCreeperAI] Player detected — entering aggro.");
            }
        }

        private void UpdateAggro()
        {
            // Movement is handled in FixedUpdate.
            // State transitions out of Aggro only via ApplySubdueDamage().
        }

        private void UpdateRestored()
        {
            _scanWindowTimer += Time.deltaTime;

            if (_scanWindowTimer >= _scanWindowDuration)
            {
                State = CreepState.PostScan;
                Debug.Log("[TarnCreeperAI] Scan window expired. Entering passive state.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Movement
        // ─────────────────────────────────────────────────────────────────────

        private void MoveTowardPlayer()
        {
            if (_playerTransform == null) return;

            Vector2 dir = ((Vector2)_playerTransform.position - _rb.position).normalized;
            _rb.linearVelocity = dir * _moveSpeed;
        }

        private void StopMovement()
        {
            _rb.linearVelocity = Vector2.zero;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Subdue — called by PulseLanceController
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by PulseLanceController when a lance hit connects.
        /// Reduces subdue health. When empty, triggers Phase 2 restoration.
        /// </summary>
        public void ApplySubdueDamage(float damage)
        {
            if (State != CreepState.Aggro) return;

            _currentSubdueHealth -= damage;
            Debug.Log($"[TarnCreeperAI] Subdue damage {damage}. " +
                      $"Remaining: {_currentSubdueHealth:F0}/{_maxSubdueHealth:F0}");

            if (_currentSubdueHealth <= 0f)
                EnterRestored();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Restoration
        // ─────────────────────────────────────────────────────────────────────

        private void EnterRestored()
        {
            State            = CreepState.Restored;
            _scanWindowTimer = 0f;
            StopMovement();

            // Future: swap SpriteRenderer colour from Toxic Amber → Neon Cyan here
            // Future: play restoration particle effect

            Debug.Log("[TarnCreeperAI] RESTORED. Scan window open for " +
                      $"{_scanWindowDuration}s — fire scanner now.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // IScannable
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// True only in Restored or PostScan state.
        /// Corrupted (Idle/Aggro) creature scanned = Common tier.
        /// Restored creature scanned = Rare tier (AliveTier from CreatureDataSO).
        /// This enforces the tame-and-scan design pillar.
        /// </summary>
        public bool IsAlive => State == CreepState.Restored || State == CreepState.PostScan;

        public CreatureDataSO GetData() => _data;

        public void OnScanBegin()
        {
            // Scan is only possible during the Restored window (collider always on,
            // but IsAlive drives tier — player CAN scan in Aggro, gets Common).
        }

        public void OnScanComplete()
        {
            if (State == CreepState.Restored)
            {
                State = CreepState.PostScan;
                Debug.Log("[TarnCreeperAI] Scan complete during restoration window. " +
                          "Biome health restoration applied via scan pipeline.");
            }
        }

        public void OnScanInterrupted() { }

        // ─────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.3f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _aggroRadius);
        }
    }
}
