using System.Collections;
using UnityEngine;
using TerrasHeart.Events;
using TerrasHeart.Scanner;

namespace TerrasHeart.Environment
{
    /// <summary>
    /// Bioluminescent cave organism (mushroom or biological spring) that launches Dr. Maria upward.
    /// Implements IScannable — scanning it yields a journal entry (science-first interaction).
    ///
    /// SETUP:
    ///   - Collider2D: BoxCollider2D, NOT trigger. Top surface triggers the launch.
    ///   - Layer: Scannable — so ScannerController's raycast detects it.
    ///   - DrMaria must have tag "Player".
    ///   - Assign JumpPadConfigSO and a CreatureDataSO (the organism's scan data).
    ///   - SpriteRenderer on same object or child — used for visual scale punch.
    ///
    /// DESIGN NOTE: This is a living organism. Scanning before using is the ideal
    /// first interaction. The scan gives a journal entry about the organism.
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

        [Header("Visual")]
        [Tooltip("SpriteRenderer to punch on launch. Can be this object's own or a child.")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        // ─────────────────────────────────────────────────────────────
        // STATE
        // ─────────────────────────────────────────────────────────────

        private bool _onCooldown;

#pragma warning disable CS0414
        private bool _beingScanned;
#pragma warning restore CS0414

        // ─────────────────────────────────────────────────────────────
        // ISCANNABLE IMPLEMENTATION
        // ─────────────────────────────────────────────────────────────

        /// <summary>Jump pad organism is always alive — scannable at any time.</summary>
        public bool IsAlive => true;

        /// <summary>Returns the organism's scan data SO.</summary>
        public CreatureDataSO GetData() => _data;

        /// <summary>Scanner beam locked on — optional: show a highlight or pulse.</summary>
        public void OnScanBegin()
        {
            _beingScanned = true;
            // Graybox: no visual. Art phase: start a gentle glow pulse coroutine.
        }

        /// <summary>Scan completed successfully.</summary>
        public void OnScanComplete()
        {
            _beingScanned = false;
            // Graybox: no visual. Art phase: brief bright flash, then settle.
        }

        /// <summary>Scan was interrupted before completing.</summary>
        public void OnScanInterrupted()
        {
            _beingScanned = false;
            // Graybox: no visual. Art phase: revert scan-begin glow.
        }

        // ─────────────────────────────────────────────────────────────
        // COLLISION — player lands on top surface
        // ─────────────────────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (_onCooldown) return;
            if (_config == null) return;
            if (!col.gameObject.CompareTag("Player")) return;

            Rigidbody2D playerRb = col.rigidbody;
            if (playerRb == null) return;

            // Only launch when player lands on the TOP surface
            foreach (var contact in col.contacts)
            {
                if (contact.normal.y > 0.7f)
                {
                    Launch(playerRb);
                    return;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // LAUNCH
        // ─────────────────────────────────────────────────────────────

        private void Launch(Rigidbody2D playerRb)
        {
            // Preserve horizontal velocity — run-up directional launches work correctly
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, _config.LaunchForce);

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
            Transform target = _spriteRenderer != null ? _spriteRenderer.transform : transform;

            target.localScale = new Vector3(1.2f, 0.7f, 1f);

            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.25f;
                target.localScale = Vector3.Lerp(new Vector3(1.2f, 0.7f, 1f), Vector3.one, t);
                yield return null;
            }

            target.localScale = Vector3.one;
        }
    }
}