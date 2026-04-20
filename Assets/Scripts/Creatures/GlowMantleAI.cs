// ─────────────────────────────────────────────────────────────────────────────
// GlowMantleAI.cs
// Path: Assets/Scripts/Creatures/GlowMantleAI.cs
// Terra's Heart — Glow-Mantle behaviour. The biological gate entity in Room 4.
//
// Attach to: the Glow-Mantle GameObject in the choke point
// Requires: Collider2D on the Scannable physics layer
//           Rigidbody2D (Kinematic) for movement
//
// Behaviour:
//   Patrol  — moves between left and right patrol bounds at patrol speed.
//   Aggro   — player entered aggro radius. Check Skin adaptation first.
//             If Skin active: ignore completely. If not: enter charge state.
//   Charge  — moves rapidly toward player. Knockback on contact.
//             Retreats to patrol zone after charge distance exceeded.
//
// The Skin check happens every frame in Patrol — as soon as the player crafts
// the adaptation and re-enters, the Glow-Mantle ignores them permanently
// (HasEffect returns true for the rest of the session).
//
// Contact "damage" in Step 5 is implemented as knockback only.
// A full health/damage system is out of scope for the graybox vertical slice.
// The knockback is strong enough to communicate the gate clearly.
//
// IScannable notes:
//   Alive scan (post-Skin, optional): Exceptional tier
//   Dead scan: Rare — but killing the gate entity is ecologically penalised
//              and loses the gate permanently (not recommended).
//   For Step 5, IsAlive is always true (no kill path via Pulse Lance).
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Adaptations;
using TerrasHeart.Scanner;

namespace TerrasHeart.Creatures
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class GlowMantleAI : MonoBehaviour, IScannable
    {
        // ─── IScannable data ──────────────────────────────────────────────────
        [Header("Scan Data")]
        [Tooltip("Assign GlowMantleData.asset — BiomeID must be 'BrineglowDescent'.")]
        [SerializeField] private CreatureDataSO _data;

        // ─── Patrol ───────────────────────────────────────────────────────────
        [Header("Patrol")]
        [Tooltip("Half-width of the patrol zone centred on the spawn position. " +
                 "Should cover the full choke point (set to ~3 for a 6-unit zone).")]
        [SerializeField] private float _patrolHalfWidth = 3f;

        [SerializeField] private float _patrolSpeed = 1.5f;

        // ─── Aggro & Charge ───────────────────────────────────────────────────
        [Header("Aggro")]
        [Tooltip("Distance at which the Glow-Mantle detects the player and checks Skin.")]
        [SerializeField] private float _aggroRadius = 4f;

        [SerializeField] private float _chargeSpeed = 7f;

        [Tooltip("How far the Glow-Mantle travels on a charge before retreating.")]
        [SerializeField] private float _chargeMaxDistance = 6f;

        [Tooltip("Knockback force applied to player on contact. " +
                 "Requires the player Rigidbody2D — applied via FindWithTag.")]
        [SerializeField] private float _knockbackForce = 12f;

        // ─── Runtime ──────────────────────────────────────────────────────────

        private enum State { Patrol, Charging, Retreating }
        private State _state = State.Patrol;

        private Vector3        _spawnPosition;
        private float          _patrolDirection = 1f;
        private Vector3        _chargeStartPosition;

        private Rigidbody2D    _rb;
        private Transform      _playerTransform;
        private Rigidbody2D    _playerRb;
        private AdaptationManager _adaptationManager;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb            = GetComponent<Rigidbody2D>();
            _rb.bodyType   = RigidbodyType2D.Kinematic;
            _spawnPosition = transform.position;
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform   = player.transform;
                _playerRb          = player.GetComponent<Rigidbody2D>();
                _adaptationManager = player.GetComponent<AdaptationManager>();
            }
            else
            {
                Debug.LogWarning("[GlowMantleAI] Player not found. Aggro disabled.");
            }
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Patrol:     UpdatePatrol();     break;
                case State.Charging:   UpdateCharging();   break;
                case State.Retreating: UpdateRetreating(); break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // State Updates
        // ─────────────────────────────────────────────────────────────────────

        private void UpdatePatrol()
        {
            // Move horizontally between patrol bounds
            float targetX = _spawnPosition.x + (_patrolDirection * _patrolHalfWidth);
            Vector3 pos   = transform.position;
            pos.x              = Mathf.MoveTowards(pos.x, targetX, _patrolSpeed * Time.deltaTime);
            transform.position = pos;

            if (Mathf.Abs(pos.x - _spawnPosition.x) >= _patrolHalfWidth - 0.05f)
                _patrolDirection *= -1f;

            // Aggro detection — only if player is in range and Skin is NOT active
            if (_playerTransform == null) return;

            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            if (dist > _aggroRadius) return;

            // Skin adaptation check — if active, remain in patrol and ignore the player
            if (_adaptationManager != null &&
                _adaptationManager.HasEffect(AdaptationEffectType.BioluminescentCamo))
            {
                return;
            }

            // No Skin — enter charge
            EnterCharge();
        }

        private void UpdateCharging()
        {
            if (_playerTransform == null)
            {
                EnterRetreat();
                return;
            }

            // Move toward player
            Vector3 dir = (_playerTransform.position - transform.position).normalized;
            transform.position += dir * _chargeSpeed * Time.deltaTime;

            // Retreat if charge distance exceeded
            if (Vector3.Distance(transform.position, _chargeStartPosition) >= _chargeMaxDistance)
                EnterRetreat();
        }

        private void UpdateRetreating()
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                _spawnPosition,
                _patrolSpeed * 2f * Time.deltaTime);

            if (Vector3.Distance(transform.position, _spawnPosition) < 0.1f)
            {
                transform.position = _spawnPosition;
                _state             = State.Patrol;
                Debug.Log("[GlowMantleAI] Returned to patrol.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Transitions
        // ─────────────────────────────────────────────────────────────────────

        private void EnterCharge()
        {
            _state               = State.Charging;
            _chargeStartPosition = transform.position;
            Debug.Log("[GlowMantleAI] Player detected without Skin — charging.");
        }

        private void EnterRetreat()
        {
            _state = State.Retreating;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Contact — Knockback
        // ─────────────────────────────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.collider.CompareTag("Player")) return;
            if (_state != State.Charging) return;

            if (_playerRb != null)
            {
                Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                _playerRb.linearVelocity = Vector2.zero;
                _playerRb.AddForce(knockbackDir * _knockbackForce, ForceMode2D.Impulse);
                Debug.Log("[GlowMantleAI] Contact — knockback applied. (Craft Skin adaptation to pass.)");
            }

            EnterRetreat();
        }

        // ─────────────────────────────────────────────────────────────────────
        // IScannable
        // ─────────────────────────────────────────────────────────────────────

        // IsAlive is always true in Step 5. The Glow-Mantle cannot be killed
        // via Pulse Lance (Pulse Lance only reaches TarnCreeperAI in Room 6).
        public bool IsAlive => true;

        public CreatureDataSO GetData() => _data;

        public void OnScanBegin() { }
        public void OnScanComplete() { }
        public void OnScanInterrupted() { }
        // ─────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying ? _spawnPosition : transform.position;

            // Patrol zone
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                origin + Vector3.left  * _patrolHalfWidth,
                origin + Vector3.right * _patrolHalfWidth);

            // Aggro radius
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _aggroRadius);
        }
    }
}
