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
//   Idle ──► Aggro      Player enters _aggroRadius.
//   Aggro ──► Restored  Subdue health reaches 0 via PulseLance hits.
//   Restored ──► PostScan  Player scans during window, or window expires.
//
// ICorruptedScannable marker: tells ScannerController that a successful scan
// of this creature is a tame-and-scan completion. ScanResult.IsRestoredCorrupted
// is set to true, and BiomeHealthManager applies CorruptedRestoration (+15)
// via HandleScanComplete rather than the standard ScanRestoration (+3).
//
// Phase B Step 1 — Bug fix:
//   Removed direct _biomeHealthManager reference and ApplyCorruptedRestoration()
//   call from OnScanComplete. All health changes now route exclusively through
//   BiomeHealthManager.HandleScanComplete via the event bus. No direct calls,
//   no double-counting.
//
// Phase B Step 5 (upcoming):
//   Three-tier scan system — pre-placement food detection, erratic vs settled
//   restoration states, tier selection by preparation level.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Scanner;

namespace TerrasHeart.Creatures
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class TarnCreeperAI : MonoBehaviour, IScannable, ICorruptedScannable
    {
        [Header("Scan Data")]
        [Tooltip("Assign TarnCreeperData.asset — BiomeID must be 'BrineglowDescent'.")]
        [SerializeField] private CreatureDataSO _data;

        [Header("Aggro")]
        [SerializeField] private float _aggroRadius = 5f;
        [SerializeField] private float _moveSpeed = 2.5f;

        [Header("Subdue")]
        [Tooltip("Total subdue damage required to trigger restoration. ~3-5 hits.")]
        [SerializeField] private float _maxSubdueHealth = 30f;

        [Header("Scan Window")]
        [Tooltip("Seconds scan window stays open after restoration. Phase A spec: 4s.")]
        [SerializeField] private float _scanWindowDuration = 4f;

        public enum CreepState { Idle, Aggro, Restored, PostScan }
        public CreepState State { get; private set; } = CreepState.Idle;

        private float _currentSubdueHealth;
        private float _scanWindowTimer;
        private Rigidbody2D _rb;
        private Transform _playerTransform;

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
                Debug.LogWarning("[TarnCreeperAI] Player not found.");
        }

        private void Update()
        {
            switch (State)
            {
                case CreepState.Idle: UpdateIdle(); break;
                case CreepState.Restored: UpdateRestored(); break;
            }
        }

        private void FixedUpdate()
        {
            if (State == CreepState.Aggro)
                MoveTowardPlayer();
        }

        // ─────────────────────────────────────────────────────────────────────
        // State Logic
        // ─────────────────────────────────────────────────────────────────────

        private void UpdateIdle()
        {
            if (_playerTransform == null) return;
            if (Vector2.Distance(transform.position, _playerTransform.position) <= _aggroRadius)
            {
                State = CreepState.Aggro;
                Debug.Log("[TarnCreeperAI] Aggro.");
            }
        }

        private void UpdateRestored()
        {
            _scanWindowTimer += Time.deltaTime;
            if (_scanWindowTimer >= _scanWindowDuration)
            {
                State = CreepState.PostScan;
                Debug.Log("[TarnCreeperAI] Scan window expired.");
            }
        }

        private void MoveTowardPlayer()
        {
            if (_playerTransform == null) return;
            Vector2 dir = ((Vector2)_playerTransform.position - _rb.position).normalized;
            _rb.linearVelocity = dir * _moveSpeed;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Combat
        // ─────────────────────────────────────────────────────────────────────

        public void ApplySubdueDamage(float damage)
        {
            if (State != CreepState.Aggro) return;
            _currentSubdueHealth -= damage;
            Debug.Log($"[TarnCreeperAI] Subdue: {_currentSubdueHealth:F0}/{_maxSubdueHealth:F0}");
            if (_currentSubdueHealth <= 0f)
            {
                State = CreepState.Restored;
                _scanWindowTimer = 0f;
                _rb.linearVelocity = Vector2.zero;
                Debug.Log($"[TarnCreeperAI] RESTORED. Scan window: {_scanWindowDuration}s.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // IScannable
        // ─────────────────────────────────────────────────────────────────────

        public bool IsAlive => State == CreepState.Restored || State == CreepState.PostScan;
        public CreatureDataSO GetData() => _data;
        public void OnScanBegin() { }
        public void OnScanInterrupted() { }

        public void OnScanComplete()
        {
            if (State != CreepState.Restored) return;
            State = CreepState.PostScan;
            // Health restoration is handled exclusively by BiomeHealthManager.HandleScanComplete
            // via the event bus. ScanResult.IsRestoredCorrupted is true because this class
            // implements ICorruptedScannable — no direct call needed here.
            Debug.Log("[TarnCreeperAI] Scan complete. State → PostScan. " +
                      "Health restoration routed through BiomeHealthManager.");
        }

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