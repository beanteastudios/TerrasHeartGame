using System.Collections;
using UnityEngine;
using TerrasHeart.Events;
using TerrasHeart.Scanner;
using TerrasHeart.Player;

namespace TerrasHeart.Environment
{
    /// <summary>
    /// Bioluminescent spring organism. Provides a boosted jump when DrMaria
    /// stands on it and presses Space — player retains full control.
    /// Implements IScannable — scanning yields a journal entry.
    ///
    /// SETUP:
    ///   Parent GameObject (JumpPad_01):
    ///     - Layer: Scannable (scanner detection)
    ///     - JumpPad component
    ///     - No collider on parent
    ///
    ///   Child GameObject (JumpPad_Ground):
    ///     - Layer: Ground (so PlayerController.IsGrounded works)
    ///     - BoxCollider2D, NOT trigger, size ~1.5 x 0.3
    ///     - No scripts
    ///
    /// The boosted jump fires when PlayerController raises OnJumpRequested
    /// while DrMaria is grounded on this pad's child collider.
    /// </summary>
    public class JumpPad : MonoBehaviour, IScannable
    {
        // ─────────────────────────────────────────────────────────────
        // SERIALIZED FIELDS
        // ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private JumpPadConfigSO _config;

        [Header("Scan Data")]
        [Tooltip("CreatureDataSO for this organism. BiomeID = BrineglowDescent, AliveTier = Uncommon.")]
        [SerializeField] private CreatureDataSO _data;

        [Header("References")]
        [Tooltip("The child GameObject that has the Ground-layer BoxCollider2D.")]
        [SerializeField] private Collider2D _groundCollider;

        [Tooltip("SpriteRenderer to punch on launch. Can be this object's own or a child.")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        // ─────────────────────────────────────────────────────────────
        // STATE
        // ─────────────────────────────────────────────────────────────

        private bool _playerOnPad;
        private Rigidbody2D _playerRb;
        private bool _onCooldown;

#pragma warning disable CS0414
        private bool _beingScanned;
#pragma warning restore CS0414

        // ─────────────────────────────────────────────────────────────
        // ISCANNABLE
        // ─────────────────────────────────────────────────────────────

        public bool IsAlive => true;
        public CreatureDataSO GetData() => _data;
        public void OnScanBegin() { _beingScanned = true; }
        public void OnScanComplete() { _beingScanned = false; }
        public void OnScanInterrupted() { _beingScanned = false; }

        // ─────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameEvents.OnJumpInput += HandleJumpInput;
        }

        private void OnDisable()
        {
            GameEvents.OnJumpInput -= HandleJumpInput;
        }

        // ─────────────────────────────────────────────────────────────
        // COLLISION — detect player standing on pad
        // ─────────────────────────────────────────────────────────────

        public void NotifyCollisionEnter(Collision2D col)
        {
            if (!col.gameObject.CompareTag("Player")) return;
            foreach (var contact in col.contacts)
            {
                if (contact.normal.y < -0.7f)
                {
                    _playerOnPad = true;
                    _playerRb = col.rigidbody;
                    return;
                }
            }
        }

        public void NotifyCollisionExit(Collision2D col)
        {
            if (!col.gameObject.CompareTag("Player")) return;
            _playerOnPad = false;
            _playerRb = null;
        }

        // ─────────────────────────────────────────────────────────────
        // JUMP INPUT — boosted launch when player is on pad
        // ─────────────────────────────────────────────────────────────

        private void HandleJumpInput()
        {
            if (!_playerOnPad || _playerRb == null || _onCooldown) return;
            if (_config == null) return;

            // Cancel the normal jump buffered in PlayerController
            // so it doesn't override our boosted launch in the same FixedUpdate
            _playerRb.GetComponent<PlayerController>()?.CancelJumpRequest();

            // Apply boosted vertical velocity, preserve horizontal
            _playerRb.linearVelocity = new Vector2(
                _playerRb.linearVelocity.x, _config.LaunchForce);

            GameEvents.RaiseJumpPadLaunched(transform.position);

            StartCoroutine(CooldownRoutine());
            StartCoroutine(VisualPunch());
        }

        // ─────────────────────────────────────────────────────────────
        // COROUTINES
        // ─────────────────────────────────────────────────────────────

        private IEnumerator CooldownRoutine()
        {
            _onCooldown = true;
            yield return new WaitForSeconds(_config.Cooldown);
            _onCooldown = false;
        }

        private IEnumerator VisualPunch()
        {
            Transform target = _spriteRenderer != null
                ? _spriteRenderer.transform : transform;

            target.localScale = new Vector3(1.2f, 0.7f, 1f);

            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.25f;
                target.localScale = Vector3.Lerp(
                    new Vector3(1.2f, 0.7f, 1f), Vector3.one, t);
                yield return null;
            }

            target.localScale = Vector3.one;
        }
    }
}