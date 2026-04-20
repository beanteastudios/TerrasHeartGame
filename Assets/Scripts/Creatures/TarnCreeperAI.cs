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
//   Idle ──► Aggro             Player enters _aggroRadius.
//   Aggro ──► RestoredErratic  Subdue health reaches 0, no food pre-placed.
//   Aggro ──► RestoredSettled  Subdue health reaches 0, food was pre-placed.
//   RestoredErratic ──► RestoredSettled  Food placed during restoration window.
//   RestoredErratic / RestoredSettled ──► PostScan  Scan completes or window expires.
//
// THREE-TIER SCAN SYSTEM:
//   Scan during RestoredErratic           → Rare         (_data)
//   Food placed during restoration window → Exceptional  (_dataExceptional)
//   Food placed before subduing           → NahiTouched  (_dataNahiTouched)
//
//   GetData() returns the correct SO based on state and preparation flag.
//   BiomeHealthManager always applies CorruptedRestoration (+15) via
//   ICorruptedScannable regardless of tier — tier only changes specimen reward.
//
// FOOD DETECTION:
//   Subscribes to GameEvents.OnFoodPlaced.
//   Food within _foodDetectionRange during Idle/Aggro → _foodPlacedBeforeRestoration = true.
//   Food within _foodDetectionRange during RestoredErratic → transitions to RestoredSettled.
//   Same Biological material cost as Cave Luminoth food throw.
//
// Phase B Step 5.
// V7 naming pass: AurTouched → NahiTouched throughout.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Scanner;
using TerrasHeart.Events;

namespace TerrasHeart.Creatures
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class TarnCreeperAI : MonoBehaviour, IScannable, ICorruptedScannable
    {
        // ─── Scan Data ────────────────────────────────────────────────────────

        [Header("Scan Data")]
        [Tooltip("Assign TarnCreeperData.asset — BiomeID 'BrineglowDescent'. " +
                 "Alive tier: Rare. Used when player scans with no preparation.")]
        [SerializeField] private CreatureDataSO _data;

        [Tooltip("Assign TarnCreeperDataExceptional.asset — Alive tier: Exceptional. " +
                 "Used when food was placed during the restoration window.")]
        [SerializeField] private CreatureDataSO _dataExceptional;

        [Tooltip("Assign TarnCreeperDataNahiTouched.asset — Alive tier: NahiTouched. " +
                 "Used when food was placed before subduing (full ecological anticipation).")]
        [SerializeField] private CreatureDataSO _dataNahiTouched;

        // ─── Aggro ────────────────────────────────────────────────────────────

        [Header("Aggro")]
        [SerializeField] private float _aggroRadius = 5f;
        [SerializeField] private float _moveSpeed = 2.5f;

        // ─── Subdue ───────────────────────────────────────────────────────────

        [Header("Subdue")]
        [Tooltip("Total subdue damage required to trigger restoration. ~3-5 hits.")]
        [SerializeField] private float _maxSubdueHealth = 30f;

        // ─── Scan Window ──────────────────────────────────────────────────────

        [Header("Scan Window")]
        [Tooltip("Seconds scan window stays open after restoration. " +
                 "Must exceed ScannerConfig.ScanHoldDuration to guarantee a full scan.")]
        [SerializeField] private float _scanWindowDuration = 6f;

        // ─── Food Detection ───────────────────────────────────────────────────

        [Header("Food Detection")]
        [Tooltip("Food must land within this distance to count as pre-placed or " +
                 "in-window placement. Should be generous — the throw arc is imprecise.")]
        [SerializeField] private float _foodDetectionRange = 5f;

        // ─── Runtime State ────────────────────────────────────────────────────

        public enum CreepState { Idle, Aggro, RestoredErratic, RestoredSettled, PostScan }
        public CreepState State { get; private set; } = CreepState.Idle;

        private float _currentSubdueHealth;
        private float _scanWindowTimer;
        private bool _foodPlacedBeforeRestoration;
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

        private void OnEnable() => GameEvents.OnFoodPlaced += HandleFoodPlaced;
        private void OnDisable() => GameEvents.OnFoodPlaced -= HandleFoodPlaced;

        private void Update()
        {
            switch (State)
            {
                case CreepState.Idle: UpdateIdle(); break;
                case CreepState.RestoredErratic: UpdateRestoredErratic(); break;
                case CreepState.RestoredSettled: UpdateRestoredSettled(); break;
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

        private void UpdateRestoredErratic()
        {
            _scanWindowTimer += Time.deltaTime;
            if (_scanWindowTimer >= _scanWindowDuration)
            {
                State = CreepState.PostScan;
                Debug.Log("[TarnCreeperAI] Scan window expired (erratic).");
            }
        }

        private void UpdateRestoredSettled()
        {
            _scanWindowTimer += Time.deltaTime;
            if (_scanWindowTimer >= _scanWindowDuration)
            {
                State = CreepState.PostScan;
                Debug.Log("[TarnCreeperAI] Scan window expired (settled).");
            }
        }

        private void MoveTowardPlayer()
        {
            if (_playerTransform == null) return;
            Vector2 dir = ((Vector2)_playerTransform.position - _rb.position).normalized;
            _rb.linearVelocity = dir * _moveSpeed;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Food Event Handler
        // ─────────────────────────────────────────────────────────────────────

        private void HandleFoodPlaced(Vector2 foodPosition)
        {
            float dist = Vector2.Distance(transform.position, foodPosition);
            if (dist > _foodDetectionRange) return;

            switch (State)
            {
                case CreepState.Idle:
                case CreepState.Aggro:
                    _foodPlacedBeforeRestoration = true;
                    Debug.Log($"[TarnCreeperAI] Food pre-placed ({dist:F1}u). " +
                              "NahiTouched tier primed.");
                    break;

                case CreepState.RestoredErratic:
                    State = CreepState.RestoredSettled;
                    _scanWindowTimer = 0f;
                    Debug.Log($"[TarnCreeperAI] Food placed during window ({dist:F1}u). " +
                              "State → RestoredSettled. Exceptional tier primed.");
                    break;
            }
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
                _rb.linearVelocity = Vector2.zero;
                _scanWindowTimer = 0f;

                if (_foodPlacedBeforeRestoration)
                {
                    State = CreepState.RestoredSettled;
                    Debug.Log($"[TarnCreeperAI] RESTORED (settled — food was pre-placed). " +
                              $"Scan window: {_scanWindowDuration}s. NahiTouched tier.");
                }
                else
                {
                    State = CreepState.RestoredErratic;
                    Debug.Log($"[TarnCreeperAI] RESTORED (erratic — no preparation). " +
                              $"Scan window: {_scanWindowDuration}s. Rare tier.");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // IScannable
        // ─────────────────────────────────────────────────────────────────────

        public bool IsAlive =>
            State == CreepState.RestoredErratic ||
            State == CreepState.RestoredSettled ||
            State == CreepState.PostScan;

        /// <summary>
        /// Returns the correct SO based on preparation tier:
        ///   RestoredSettled + food pre-placed  → NahiTouched  (_dataNahiTouched)
        ///   RestoredSettled + in-window food   → Exceptional  (_dataExceptional)
        ///   RestoredErratic                    → Rare         (_data)
        ///   Any other state                    → Rare         (_data)
        /// Falls back to _data if the relevant SO is unassigned.
        /// </summary>
        public CreatureDataSO GetData()
        {
            if (State == CreepState.RestoredSettled)
            {
                if (_foodPlacedBeforeRestoration)
                    return _dataNahiTouched != null ? _dataNahiTouched : _data;
                else
                    return _dataExceptional != null ? _dataExceptional : _data;
            }

            return _data;
        }

        public void OnScanBegin() { }
        public void OnScanInterrupted() { }

        public void OnScanComplete()
        {
            if (State != CreepState.RestoredErratic && State != CreepState.RestoredSettled)
                return;

            string tier = State == CreepState.RestoredSettled
                ? (_foodPlacedBeforeRestoration ? "NahiTouched" : "Exceptional")
                : "Rare";

            State = CreepState.PostScan;
            Debug.Log($"[TarnCreeperAI] Scan complete — {tier} tier. State → PostScan. " +
                      "Health restoration routed through BiomeHealthManager.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.3f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _aggroRadius);

            Gizmos.color = new Color(1f, 0.6f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _foodDetectionRange);
        }
    }
}