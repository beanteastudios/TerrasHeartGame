// ─────────────────────────────────────────────────────────────────────────────
// GlowMantleAI.cs
// Path: Assets/Scripts/Creatures/GlowMantleAI.cs
// Terra's Heart — Glow-Mantle behaviour. The biological gate entity in Room 4.
//
// Attach to: the Glow-Mantle GameObject in the choke point
// Requires: Collider2D on the Scannable physics layer
//           Rigidbody2D (Kinematic) for movement
//
// STATE MACHINE:
//   Patrol          — moves between patrol bounds. Checks for aggro or call trigger.
//   Charging        — charges toward player (no SkinCamo in range).
//   Retreating      — returns to spawn after charge.
//   Broadcasting    — pulses colour sequence on SpriteRenderer. No interaction.
//   WaitingForInput — input window open. Player mirrors the sequence with 1/2 keys.
//   Receptive       — correct match. Scan window open. Exceptional tier available.
//   Dismissed       — wrong input or timeout. Cold dismissal. Back to Patrol.
//
// GATE LOGIC:
//   Without SkinCamo — creature charges. Player cannot pass the choke point.
//   With SkinCamo    — creature ignores player in Patrol/Retreating.
//                     Call-and-response triggers when player enters
//                     CallAndResponseRange.
//
// SCAN TIERS:
//   Receptive state scan → Exceptional (uses _dataReceptive SO)
//   Any other state scan → Rare alive  (uses _data SO)
//   GetData() returns the correct asset based on current state.
//
// Phase B Step 4.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using TerrasHeart.Adaptations;
using TerrasHeart.Scanner;
using TerrasHeart.Events;

namespace TerrasHeart.Creatures
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class GlowMantleAI : MonoBehaviour, IScannable
    {
        // ─── Scan Data ────────────────────────────────────────────────────────

        [Header("Scan Data")]
        [Tooltip("Assign GlowMantleData.asset — BiomeID must be 'BrineglowDescent'. " +
                 "Alive tier: Rare. Used for all scans outside Receptive state.")]
        [SerializeField] private CreatureDataSO _data;

        [Tooltip("Assign GlowMantleDataReceptive.asset — Alive tier: Exceptional. " +
                 "Used only when player scans during the Receptive state after a " +
                 "correct call-and-response match.")]
        [SerializeField] private CreatureDataSO _dataReceptive;

        // ─── Config ───────────────────────────────────────────────────────────

        [Header("Call-and-Response Config")]
        [Tooltip("Assign GlowMantleConfig.asset from Assets/Data/ScriptableObjects/Creatures/")]
        [SerializeField] private GlowMantleConfigSO _config;

        [Header("HUD")]
        [Tooltip("Assign the GlowMantleHUDController component on the Canvas child.")]
        [SerializeField] private GlowMantleHUDController _hud;

        // ─── Patrol ───────────────────────────────────────────────────────────

        [Header("Patrol")]
        [Tooltip("Half-width of the patrol zone centred on the spawn position.")]
        [SerializeField] private float _patrolHalfWidth = 3f;
        [SerializeField] private float _patrolSpeed = 1.5f;

        // ─── Aggro & Charge ───────────────────────────────────────────────────

        [Header("Aggro")]
        [Tooltip("Distance at which the Glow-Mantle detects the player and checks Skin.")]
        [SerializeField] private float _aggroRadius = 4f;
        [SerializeField] private float _chargeSpeed = 7f;

        [Tooltip("How far the Glow-Mantle travels on a charge before retreating.")]
        [SerializeField] private float _chargeMaxDistance = 6f;

        [Tooltip("Knockback force applied to player on contact.")]
        [SerializeField] private float _knockbackForce = 12f;

        // ─── Runtime State ────────────────────────────────────────────────────

        private enum GlowState
        {
            Patrol, Charging, Retreating,
            Broadcasting, WaitingForInput, Receptive, Dismissed
        }

        private GlowState _state = GlowState.Patrol;

        private Vector3 _spawnPosition;
        private float _patrolDirection = 1f;
        private Vector3 _chargeStartPosition;

        private int _inputStep;
        private float _inputTimer;
        private float _receptiveTimer;
        private float _dismissedTimer;

        private Color _baseColour = Color.white;

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Transform _playerTransform;
        private Rigidbody2D _playerRb;
        private AdaptationManager _adaptationManager;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _spawnPosition = transform.position;

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null)
                _baseColour = _spriteRenderer.color;
            else
                Debug.LogWarning("[GlowMantleAI] No SpriteRenderer found — " +
                                 "colour pulse will not be visible.");
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                _playerRb = player.GetComponent<Rigidbody2D>();
                _adaptationManager = player.GetComponent<AdaptationManager>();
            }
            else
            {
                Debug.LogWarning("[GlowMantleAI] Player not found. Aggro disabled.");
            }

            if (_config != null && _hud != null)
                _hud.Initialise(_config.CyanColour, _config.AmberColour);
        }

        private void OnEnable() => GameEvents.OnPaletteInput += HandlePaletteInput;
        private void OnDisable()
        {
            GameEvents.OnPaletteInput -= HandlePaletteInput;
            StopAllCoroutines();
        }

        private void Update()
        {
            switch (_state)
            {
                case GlowState.Patrol: UpdatePatrol(); break;
                case GlowState.Charging: UpdateCharging(); break;
                case GlowState.Retreating: UpdateRetreating(); break;
                case GlowState.WaitingForInput: UpdateWaitingForInput(); break;
                case GlowState.Receptive: UpdateReceptive(); break;
                case GlowState.Dismissed: UpdateDismissed(); break;
                    // Broadcasting is coroutine-driven — no Update logic needed.
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // State Updates
        // ─────────────────────────────────────────────────────────────────────

        private void UpdatePatrol()
        {
            float targetX = _spawnPosition.x + (_patrolDirection * _patrolHalfWidth);
            Vector3 pos = transform.position;
            pos.x = Mathf.MoveTowards(pos.x, targetX, _patrolSpeed * Time.deltaTime);
            transform.position = pos;

            if (Mathf.Abs(pos.x - _spawnPosition.x) >= _patrolHalfWidth - 0.05f)
                _patrolDirection *= -1f;

            if (_playerTransform == null) return;

            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            bool hasSkin = _adaptationManager != null &&
                            _adaptationManager.HasEffect(AdaptationEffectType.BioluminescentCamo);

            if (dist <= _aggroRadius && !hasSkin)
            {
                EnterCharge();
                return;
            }

            if (dist <= GetCallAndResponseRange() && hasSkin)
                EnterBroadcasting();
        }

        private void UpdateCharging()
        {
            if (_playerTransform == null) { EnterRetreat(); return; }

            Vector3 dir = (_playerTransform.position - transform.position).normalized;
            transform.position += dir * _chargeSpeed * Time.deltaTime;

            if (Vector3.Distance(transform.position, _chargeStartPosition) >= _chargeMaxDistance)
                EnterRetreat();
        }

        private void UpdateRetreating()
        {
            transform.position = Vector3.MoveTowards(
                transform.position, _spawnPosition, _patrolSpeed * 2f * Time.deltaTime);

            if (Vector3.Distance(transform.position, _spawnPosition) < 0.1f)
            {
                transform.position = _spawnPosition;
                _state = GlowState.Patrol;
                Debug.Log("[GlowMantleAI] Returned to patrol.");
            }
        }

        private void UpdateWaitingForInput()
        {
            if (_config == null) return;

            _inputTimer += Time.deltaTime;
            if (_inputTimer >= _config.InputTimePerPulse)
            {
                Debug.Log("[GlowMantleAI] Input window expired — dismissed.");
                EnterDismissed();
            }
        }

        private void UpdateReceptive()
        {
            _receptiveTimer += Time.deltaTime;
            if (_receptiveTimer >= GetReceptiveDuration())
            {
                Debug.Log("[GlowMantleAI] Receptive window expired. Returning to patrol.");
                ReturnToPatrol();
            }
        }

        private void UpdateDismissed()
        {
            _dismissedTimer += Time.deltaTime;
            if (_dismissedTimer >= GetDismissedDuration())
            {
                Debug.Log("[GlowMantleAI] Dismissal complete. Returning to patrol.");
                ReturnToPatrol();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Transitions
        // ─────────────────────────────────────────────────────────────────────

        private void EnterCharge()
        {
            _state = GlowState.Charging;
            _chargeStartPosition = transform.position;
            Debug.Log("[GlowMantleAI] Player detected without Skin — charging.");
        }

        private void EnterRetreat()
        {
            _state = GlowState.Retreating;
        }

        private void EnterBroadcasting()
        {
            _state = GlowState.Broadcasting;
            Debug.Log("[GlowMantleAI] SkinCamo detected — starting broadcast sequence.");
            StartCoroutine(BroadcastSequence());
        }

        private void EnterDismissed()
        {
            _state = GlowState.Dismissed;
            _dismissedTimer = 0f;
            SetSpriteColour(_baseColour);
            _hud?.SetStatus("pattern reset");
            _hud?.Hide();
            Debug.Log("[GlowMantleAI] Cold dismissal.");
        }

        private void ReturnToPatrol()
        {
            _state = GlowState.Patrol;
            SetSpriteColour(_baseColour);
            _hud?.ResetSlots();
            _hud?.Hide();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Broadcast Sequence Coroutine
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator BroadcastSequence()
        {
            if (_config == null)
            {
                Debug.LogWarning("[GlowMantleAI] GlowMantleConfigSO not assigned.");
                _state = GlowState.Patrol;
                yield break;
            }

            foreach (int step in _config.Sequence)
            {
                Color pulseColour = step == 0 ? _config.CyanColour : _config.AmberColour;

                SetSpriteColour(pulseColour);
                yield return new WaitForSeconds(_config.PulseDuration);

                SetSpriteColour(_baseColour);
                yield return new WaitForSeconds(_config.PauseBetweenPulses);
            }

            yield return new WaitForSeconds(_config.PostSequencePause);

            // Transition to input window
            _state = GlowState.WaitingForInput;
            _inputStep = 0;
            _inputTimer = 0f;

            _hud?.ResetSlots();
            _hud?.SetStatus("mimic the signal");
            _hud?.Show();

            Debug.Log("[GlowMantleAI] Sequence broadcast complete. Input window open.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Palette Input Handler
        // ─────────────────────────────────────────────────────────────────────

        private void HandlePaletteInput(int index)
        {
            if (_state != GlowState.WaitingForInput) return;
            if (_config == null) return;

            int expected = _config.Sequence[_inputStep];

            if (index == expected)
            {
                // Correct input
                Color fillColour = index == 0 ? _config.CyanColour : _config.AmberColour;
                _hud?.FillSlot(_inputStep, fillColour);

                _inputStep++;
                _inputTimer = 0f;

                Debug.Log($"[GlowMantleAI] Correct input {index} at step {_inputStep - 1}.");

                if (_inputStep >= _config.Sequence.Length)
                    EnterReceptive();
            }
            else
            {
                // Wrong input
                Debug.Log($"[GlowMantleAI] Wrong input {index} — expected {expected}. Dismissed.");
                EnterDismissed();
            }
        }

        private void EnterReceptive()
        {
            _state = GlowState.Receptive;
            _receptiveTimer = 0f;

            SetSpriteColour(_config != null ? _config.CyanColour : Color.cyan);
            _hud?.SetStatus("signal matched — scan window open");

            Debug.Log("[GlowMantleAI] Signal matched. Receptive. Scan window open — Exceptional tier.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Contact — Knockback
        // ─────────────────────────────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.collider.CompareTag("Player")) return;
            if (_state != GlowState.Charging) return;

            if (_playerRb != null)
            {
                Vector2 dir = (collision.transform.position - transform.position).normalized;
                _playerRb.linearVelocity = Vector2.zero;
                _playerRb.AddForce(dir * _knockbackForce, ForceMode2D.Impulse);
                Debug.Log("[GlowMantleAI] Contact — knockback applied. " +
                          "(Craft Skin adaptation to pass.)");
            }

            EnterRetreat();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void SetSpriteColour(Color colour)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = colour;
        }

        private float GetCallAndResponseRange() =>
            _config != null ? _config.CallAndResponseRange : 5f;

        private float GetReceptiveDuration() =>
            _config != null ? _config.ReceptiveDuration : 5f;

        private float GetDismissedDuration() =>
            _config != null ? _config.DismissedDuration : 2.5f;

        // ─────────────────────────────────────────────────────────────────────
        // IScannable
        // ─────────────────────────────────────────────────────────────────────

        public bool IsAlive => true;

        /// <summary>
        /// Returns the Exceptional-tier SO during Receptive state.
        /// Returns the standard Rare-tier SO in all other states.
        /// Drives scan tier without additional interfaces.
        /// </summary>
        public CreatureDataSO GetData() =>
            _state == GlowState.Receptive && _dataReceptive != null
                ? _dataReceptive
                : _data;

        public void OnScanBegin() { }
        public void OnScanInterrupted() { }

        public void OnScanComplete()
        {
            if (_state == GlowState.Receptive)
            {
                Debug.Log("[GlowMantleAI] Scanned in Receptive state — Exceptional tier.");
                ReturnToPatrol();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying ? _spawnPosition : transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                origin + Vector3.left * _patrolHalfWidth,
                origin + Vector3.right * _patrolHalfWidth);

            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _aggroRadius);

            Gizmos.color = new Color(0f, 1f, 0.82f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, GetCallAndResponseRange());
        }
    }
}