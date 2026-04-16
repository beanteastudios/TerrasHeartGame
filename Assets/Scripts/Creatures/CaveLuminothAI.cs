// ─────────────────────────────────────────────────────────────────────────────
// CaveLuminothAI.cs
// Path: Assets/Scripts/Creatures/CaveLuminothAI.cs
// Terra's Heart — Cave Luminoth behaviour for Brineglow Descent.
//
// Attach to: each Cave Luminoth GameObject
// Requires: Collider2D on the Scannable physics layer (for scanner raycast)
//
// Behaviour:
//   Patrol — hovers back and forth between two points at patrol speed.
//   Flee   — if player enters detection radius WITH line-of-sight, flees away.
//   Reset  — after _resetDelay seconds, returns to patrol position and resumes.
//
// The line-of-sight check is the core of the scan puzzle mechanic.
// Cover objects (stalactite columns, rock formations) block the Linecast and
// prevent the flee state from triggering — the player can hide behind them.
// The _losLayerMask must include whatever layers cover geometry sits on
// (Default / Ground are typical). Scannable layer is excluded so the
// creature's own collider doesn't block the check.
//
// IScannable notes:
//   IsAlive  — always true. Cave Luminoth cannot be killed in Step 5.
//   GetData  — assign CaveLuminothData.asset in the Inspector.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Scanner;

namespace TerrasHeart.Creatures
{
    [RequireComponent(typeof(Collider2D))]
    public class CaveLuminothAI : MonoBehaviour, IScannable
    {
        // ─── IScannable data ──────────────────────────────────────────────────
        [Header("Scan Data")]
        [Tooltip("Assign CaveLuminothData.asset — BiomeID must be 'BrineglowDescent'.")]
        [SerializeField] private CreatureDataSO _data;

        // ─── Patrol ───────────────────────────────────────────────────────────
        [Header("Patrol")]
        [Tooltip("Distance the Luminoth travels left and right from its spawn point.")]
        [SerializeField] private float _patrolDistance = 2f;

        [SerializeField] private float _patrolSpeed = 1.2f;

        [Tooltip("Vertical bob amplitude while patrolling — gives the hover feel.")]
        [SerializeField] private float _bobAmplitude = 0.15f;

        [SerializeField] private float _bobFrequency = 1.5f;

        // ─── Detection & Flee ─────────────────────────────────────────────────
        [Header("Detection")]
        [Tooltip("Radius within which the Luminoth checks for the player.")]
        [SerializeField] private float _detectionRadius = 3f;

        [Tooltip("Layers that block line of sight. Include Default/Ground geometry. " +
                 "Exclude Scannable so the creature's own collider is ignored.")]
        [SerializeField] private LayerMask _losLayerMask;

        [Header("Flee")]
        [SerializeField] private float _fleeSpeed = 4f;

        [Tooltip("How far the Luminoth flees before stopping.")]
        [SerializeField] private float _fleeDistance = 5f;

        [Tooltip("Seconds after reaching flee destination before resuming patrol.")]
        [SerializeField] private float _resetDelay = 5f;

        // ─── Runtime ──────────────────────────────────────────────────────────

        private enum State { Patrol, Flee, Resetting }
        private State   _state          = State.Patrol;

        private Vector3 _spawnPosition;
        private float   _patrolDirection = 1f;   // 1 = right, -1 = left
        private float   _bobTimer;

        private Vector3 _fleeTarget;
        private float   _resetTimer;

        private Transform _playerTransform;
        private bool      _isBeingScanned;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _spawnPosition = transform.position;
        }

        private void Start()
        {
            // Cache player transform. DrMaria persists via PersistentEntity so
            // FindWithTag is reliable in any scene.
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
            else
                Debug.LogWarning("[CaveLuminothAI] Player not found. Detection disabled.");
        }

        private void Update()
        {
            _bobTimer += Time.deltaTime;

            switch (_state)
            {
                case State.Patrol:    UpdatePatrol();    break;
                case State.Flee:      UpdateFlee();      break;
                case State.Resetting: UpdateResetting(); break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // State Updates
        // ─────────────────────────────────────────────────────────────────────

        private void UpdatePatrol()
        {
            // Bob vertically
            float bobOffset = Mathf.Sin(_bobTimer * _bobFrequency) * _bobAmplitude;

            // Move horizontally
            float targetX = _spawnPosition.x + (_patrolDirection * _patrolDistance);
            float newX    = Mathf.MoveTowards(
                transform.position.x,
                targetX,
                _patrolSpeed * Time.deltaTime);

            transform.position = new Vector3(newX, _spawnPosition.y + bobOffset, transform.position.z);

            // Reverse direction at patrol bounds
            if (Mathf.Abs(transform.position.x - _spawnPosition.x) >= _patrolDistance - 0.05f)
                _patrolDirection *= -1f;

            // Detection check every frame — cheap enough at typical creature counts
            if (PlayerIsDetected())
                EnterFlee();
        }

        private void UpdateFlee()
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                _fleeTarget,
                _fleeSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _fleeTarget) < 0.1f)
            {
                _state      = State.Resetting;
                _resetTimer = 0f;
                Debug.Log("[CaveLuminothAI] Reached flee destination. Waiting to reset.");
            }
        }

        private void UpdateResetting()
        {
            _resetTimer += Time.deltaTime;
            if (_resetTimer >= _resetDelay)
            {
                // Snap back to spawn and resume patrol
                transform.position = _spawnPosition;
                _state             = State.Patrol;
                _bobTimer          = 0f;
                Debug.Log("[CaveLuminothAI] Reset. Resuming patrol.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Detection
        // ─────────────────────────────────────────────────────────────────────

        private bool PlayerIsDetected()
        {
            if (_playerTransform == null) return false;

            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            if (dist > _detectionRadius) return false;

            // Line-of-sight check — blocked by cover geometry
            Vector2 origin    = transform.position;
            Vector2 playerPos = _playerTransform.position;
            RaycastHit2D hit  = Physics2D.Linecast(origin, playerPos, _losLayerMask);

            // If Linecast hits something before reaching the player, LOS is blocked — no flee
            if (hit.collider != null) return false;

            return true;
        }

        private void EnterFlee()
        {
            _state = State.Flee;

            // Flee directly away from the player along the horizontal axis
            Vector3 awayDir = (transform.position - _playerTransform.position).normalized;
            awayDir.y       = 0.5f; // slight upward component — ceiling retreat
            _fleeTarget     = transform.position + awayDir * _fleeDistance;

            Debug.Log("[CaveLuminothAI] Player detected — fleeing.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // IScannable
        // ─────────────────────────────────────────────────────────────────────

        public bool IsAlive => true;  // Cannot be killed in Step 5

        public CreatureDataSO GetData() => _data;

        public void OnScanBegin()
        {
            _isBeingScanned = true;
            // Future: trigger "being scanned" visual state
        }

        public void OnScanComplete()
        {
            _isBeingScanned = false;
            // Future: trigger post-scan reaction (brief light pulse, then resume)
        }

        public void OnScanInterrupted()
        {
            _isBeingScanned = false;
            // Future: revert "being scanned" visual state
        }

        // ─────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying ? _spawnPosition : transform.position;

            // Patrol range
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                origin + Vector3.left  * _patrolDistance,
                origin + Vector3.right * _patrolDistance);

            // Detection radius
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        }
    }
}
