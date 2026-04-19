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
//   Flee   — if player moves faster than _slowSpeedThreshold within detection
//            radius, flees away.
//   Reset  — after _resetDelay seconds, returns to patrol position and resumes.
//
// Movement detection:
//   Player speed is smoothed over frames to prevent single-frame spikes
//   triggering flee during slow walk. _slowSpeedThreshold must stay above
//   _slowWalkSpeed in PlayerController (default 1.2) and below _moveSpeed (5).
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
        [Tooltip("Assign CaveLuminothData.asset — BiomeID must match scene BiomeID.")]
        [SerializeField] private CreatureDataSO _data;

        // ─── Patrol ───────────────────────────────────────────────────────────
        [Header("Patrol")]
        [SerializeField] private float _patrolDistance = 4f;
        [SerializeField] private float _patrolSpeed = 1.2f;

        [Tooltip("Vertical bob amplitude while patrolling.")]
        [SerializeField] private float _bobAmplitude = 0.15f;
        [SerializeField] private float _bobFrequency = 1.5f;

        // ─── Detection ────────────────────────────────────────────────────────
        [Header("Detection")]
        [Tooltip("Radius within which the Luminoth checks player movement speed.")]
        [SerializeField] private float _detectionRadius = 3f;

        [Tooltip("Player speed above this value triggers flee. " +
                 "Must be above PlayerController slow walk speed (1.2) " +
                 "and below normal move speed (5).")]
        [SerializeField] private float _slowSpeedThreshold = 1.5f;

        // ─── Flee ─────────────────────────────────────────────────────────────
        [Header("Flee")]
        [SerializeField] private float _fleeSpeed = 4f;
        [SerializeField] private float _fleeDistance = 5f;

        [Tooltip("Seconds after reaching flee destination before resuming patrol.")]
        [SerializeField] private float _resetDelay = 8f;

        // ─── Runtime ──────────────────────────────────────────────────────────

        private enum State { Patrol, Flee, Resetting }
        private State _state = State.Patrol;

        private Vector3 _spawnPosition;
        private float _patrolDirection = 1f;
        private float _bobTimer;

        private Vector3 _fleeTarget;
        private float _resetTimer;

        private Transform _playerTransform;
        private Vector3 _playerPreviousPosition;
        private float _smoothedPlayerSpeed;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _spawnPosition = transform.position;
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                _playerPreviousPosition = player.transform.position;
            }
            else
            {
                Debug.LogWarning("[CaveLuminothAI] Player not found. Detection disabled.");
            }
        }

        private void Update()
        {
            _bobTimer += Time.deltaTime;
            TrackPlayerSpeed();

            switch (_state)
            {
                case State.Patrol: UpdatePatrol(); break;
                case State.Flee: UpdateFlee(); break;
                case State.Resetting: UpdateResetting(); break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Player Speed Tracking
        // ─────────────────────────────────────────────────────────────────────

        private void TrackPlayerSpeed()
        {
            if (_playerTransform == null) return;

            float rawSpeed = Vector3.Distance(
                _playerTransform.position,
                _playerPreviousPosition) / Time.deltaTime;

            // Smooth over frames to prevent single-frame spikes triggering flee
            _smoothedPlayerSpeed = Mathf.Lerp(_smoothedPlayerSpeed, rawSpeed, 0.2f);
            _playerPreviousPosition = _playerTransform.position;
        }

        // ─────────────────────────────────────────────────────────────────────
        // State Updates
        // ─────────────────────────────────────────────────────────────────────

        private void UpdatePatrol()
        {
            float bobOffset = Mathf.Sin(_bobTimer * _bobFrequency) * _bobAmplitude;

            float targetX = _spawnPosition.x + (_patrolDirection * _patrolDistance);
            float newX = Mathf.MoveTowards(
                transform.position.x,
                targetX,
                _patrolSpeed * Time.deltaTime);

            transform.position = new Vector3(
                newX,
                _spawnPosition.y + bobOffset,
                transform.position.z);

            if (Mathf.Abs(transform.position.x - _spawnPosition.x) >= _patrolDistance - 0.05f)
                _patrolDirection *= -1f;

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
                _state = State.Resetting;
                _resetTimer = 0f;
                Debug.Log("[CaveLuminothAI] Reached flee destination. Waiting to reset.");
            }
        }

        private void UpdateResetting()
        {
            _resetTimer += Time.deltaTime;
            if (_resetTimer >= _resetDelay)
            {
                transform.position = _spawnPosition;
                _state = State.Patrol;
                _bobTimer = 0f;
                _smoothedPlayerSpeed = 0f;
                _playerPreviousPosition = _playerTransform != null
                    ? _playerTransform.position : Vector3.zero;
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

            return _smoothedPlayerSpeed > _slowSpeedThreshold;
        }

        private void EnterFlee()
        {
            _state = State.Flee;

            Vector3 awayDir = (transform.position - _playerTransform.position).normalized;
            awayDir.y = 0.5f;
            _fleeTarget = transform.position + awayDir * _fleeDistance;

            Debug.Log("[CaveLuminothAI] Movement detected — fleeing.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // IScannable
        // ─────────────────────────────────────────────────────────────────────

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

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                origin + Vector3.left * _patrolDistance,
                origin + Vector3.right * _patrolDistance);

            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        }
    }
}