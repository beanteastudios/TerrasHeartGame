// ─────────────────────────────────────────────────────────────────────────────
// CaveLuminothAI.cs
// Path: Assets/Scripts/Creatures/CaveLuminothAI.cs
// Terra's Heart — Cave Luminoth behaviour for Brineglow Descent.
//
// Attach to: each Cave Luminoth GameObject
// Requires: Collider2D on the Scannable physics layer (for scanner raycast)
//
// Behaviour:
//   Patrol         — hovers back and forth between two points at patrol speed.
//   Flee           — if player moves faster than _slowSpeedThreshold within
//                    detection radius, OR if food lands within _frightenRadius.
//   Resetting      — waits _resetDelay seconds then returns to spawn and patrols.
//   ApproachingFood — moves horizontally toward food position at hover height.
//   Feeding        — hovers above food with slow calm bob. Scan window open.
//                    Returns to patrol when _feedingDuration expires.
//
// Food / comfort mechanic:
//   Player throws food (T key → ThrowController → FoodMarker placed).
//   GameEvents.OnFoodPlaced fires with the food's world position.
//   If food lands within _frightenRadius of the creature: flee triggers.
//   If food lands within _comfortRange and outside _frightenRadius: approach.
//   Creature moves to hover above food and enters Feeding state.
//   In Feeding state the creature is still, scan succeeds at Rare minimum
//   (IsAlive = true, creature accessible to scanner raycast as normal).
//
// Phase B Step 3.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Scanner;
using TerrasHeart.Events;

namespace TerrasHeart.Creatures
{
    [RequireComponent(typeof(Collider2D))]
    public class CaveLuminothAI : MonoBehaviour, IScannable
    {
        // ─── Scan Data ────────────────────────────────────────────────────────

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

        // ─── Food / Comfort ───────────────────────────────────────────────────

        [Header("Food / Comfort")]
        [Tooltip("Food landing within this distance of the creature triggers flee. " +
                 "Tossing directly at the creature should always frighten it.")]
        [SerializeField] private float _frightenRadius = 1.8f;

        [Tooltip("Maximum distance at which the creature detects and approaches food.")]
        [SerializeField] private float _comfortRange = 7f;

        [Tooltip("Movement speed when approaching food — slower and deliberate " +
                 "than normal patrol to signal calm intent.")]
        [SerializeField] private float _approachSpeed = 0.7f;

        [Tooltip("Seconds the creature stays in feeding state before resuming patrol. " +
                 "Should be longer than ScannerConfig.ScanHoldDuration to guarantee " +
                 "the player has a full scan window.")]
        [SerializeField] private float _feedingDuration = 6f;

        [Tooltip("Bob frequency multiplier while feeding. Lower = slower, calmer bob.")]
        [SerializeField] private float _feedingBobMultiplier = 0.25f;

        // ─── Runtime State ────────────────────────────────────────────────────

        private enum LuminothState { Patrol, Flee, Resetting, ApproachingFood, Feeding }
        private LuminothState _state = LuminothState.Patrol;

        private Vector3 _spawnPosition;
        private float _patrolDirection = 1f;
        private float _bobTimer;

        private Vector3 _fleeTarget;
        private float _resetTimer;

        private Vector2 _foodPosition;
        private float _feedingTimer;

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

        private void OnEnable()
        {
            GameEvents.OnFoodPlaced += HandleFoodPlaced;
        }

        private void OnDisable()
        {
            GameEvents.OnFoodPlaced -= HandleFoodPlaced;
        }

        private void Update()
        {
            _bobTimer += Time.deltaTime;
            TrackPlayerSpeed();

            switch (_state)
            {
                case LuminothState.Patrol: UpdatePatrol(); break;
                case LuminothState.Flee: UpdateFlee(); break;
                case LuminothState.Resetting: UpdateResetting(); break;
                case LuminothState.ApproachingFood: UpdateApproachingFood(); break;
                case LuminothState.Feeding: UpdateFeeding(); break;
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
                transform.position.x, targetX, _patrolSpeed * Time.deltaTime);

            transform.position = new Vector3(
                newX, _spawnPosition.y + bobOffset, transform.position.z);

            if (Mathf.Abs(transform.position.x - _spawnPosition.x) >= _patrolDistance - 0.05f)
                _patrolDirection *= -1f;

            if (PlayerIsDetected())
                EnterFlee();
        }

        private void UpdateFlee()
        {
            transform.position = Vector3.MoveTowards(
                transform.position, _fleeTarget, _fleeSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _fleeTarget) < 0.1f)
            {
                _state = LuminothState.Resetting;
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
                _state = LuminothState.Patrol;
                _bobTimer = 0f;
                _smoothedPlayerSpeed = 0f;
                _playerPreviousPosition = _playerTransform != null
                    ? _playerTransform.position : Vector3.zero;
                Debug.Log("[CaveLuminothAI] Reset. Resuming patrol.");
            }
        }

        private void UpdateApproachingFood()
        {
            // Approach food horizontally, maintaining spawn hover height.
            float bobOffset = Mathf.Sin(_bobTimer * _bobFrequency * 0.5f) * _bobAmplitude * 0.6f;
            float targetX = _foodPosition.x;
            float newX = Mathf.MoveTowards(
                transform.position.x, targetX, _approachSpeed * Time.deltaTime);

            transform.position = new Vector3(
                newX, _spawnPosition.y + bobOffset, transform.position.z);

            // Reached food — horizontal distance only, creature hovers above ground.
            if (Mathf.Abs(transform.position.x - _foodPosition.x) < 0.5f)
            {
                _state = LuminothState.Feeding;
                _feedingTimer = 0f;
                Debug.Log("[CaveLuminothAI] Reached food — feeding. Scan window open.");
            }
        }

        private void UpdateFeeding()
        {
            // Slow, calm bob while hovering above food. Creature is still — scan window open.
            float bobOffset = Mathf.Sin(_bobTimer * _bobFrequency * _feedingBobMultiplier)
                              * _bobAmplitude;

            transform.position = new Vector3(
                _foodPosition.x,
                _spawnPosition.y + bobOffset,
                transform.position.z);

            _feedingTimer += Time.deltaTime;
            if (_feedingTimer >= _feedingDuration)
            {
                _state = LuminothState.Patrol;
                _bobTimer = 0f;
                _smoothedPlayerSpeed = 0f;
                Debug.Log("[CaveLuminothAI] Feeding complete. Resuming patrol.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Food Event Handler
        // ─────────────────────────────────────────────────────────────────────

        private void HandleFoodPlaced(Vector2 foodPosition)
        {
            // Creature is already fleeing or resetting — too agitated to respond.
            if (_state == LuminothState.Flee || _state == LuminothState.Resetting) return;

            float distToFood = Vector2.Distance(transform.position, foodPosition);

            // Food landed too close — frighten regardless of current state.
            if (distToFood < _frightenRadius)
            {
                EnterFlee();
                Debug.Log($"[CaveLuminothAI] Food too close ({distToFood:F1}u) — frightened.");
                return;
            }

            // Already feeding — ignore subsequent throws.
            if (_state == LuminothState.Feeding || _state == LuminothState.ApproachingFood) return;

            // Food in range — approach.
            if (distToFood <= _comfortRange)
            {
                _foodPosition = foodPosition;
                _state = LuminothState.ApproachingFood;
                Debug.Log($"[CaveLuminothAI] Food detected ({distToFood:F1}u) — approaching.");
            }
            else
            {
                Debug.Log($"[CaveLuminothAI] Food out of range ({distToFood:F1}u). Ignoring.");
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
            _state = LuminothState.Flee;

            Vector3 awayDir = (transform.position - _playerTransform.position).normalized;
            awayDir.y = 0.5f;
            _fleeTarget = transform.position + awayDir * _fleeDistance;

            Debug.Log("[CaveLuminothAI] Fleeing.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // IScannable
        // ─────────────────────────────────────────────────────────────────────

        public bool IsAlive => true;
        public CreatureDataSO GetData() => _data;

        public void OnScanBegin() { }
        public void OnScanInterrupted() { }

        public void OnScanComplete()
        {
            // Scan succeeded. If feeding, log it — state continues until timer expires.
            // No state change needed: the creature finishes its food naturally.
            if (_state == LuminothState.Feeding)
                Debug.Log("[CaveLuminothAI] Scanned while feeding. State continues.");
        }

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

            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _comfortRange);

            Gizmos.color = new Color(1f, 0.2f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _frightenRadius);
        }
    }
}