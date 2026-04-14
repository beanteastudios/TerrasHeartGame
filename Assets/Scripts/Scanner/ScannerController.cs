// ─────────────────────────────────────────────────────────────────────────────
// ScannerController.cs
// Path: Assets/Scripts/Scanner/ScannerController.cs
// Terra's Heart — Dr. Maria's scanner. The primary verb of the entire game.
//
// Attach to: DrMaria (same GameObject as PlayerController)
// Requires:  Rigidbody2D (already present on DrMaria)
//
// Input style: Keyboard.current polling — matches existing PlayerController.cs.
// Scan key: F
// Facing direction: tracked from A/D key state, same as PlayerController logic.
//
// Option B note: When the .inputactions asset is created (dedicated session),
// replace Keyboard.current references with InputActionReference fields.
// The scan logic (DetectTarget, CompleteScan, ResetScanState) is unchanged.
//
// Flow:
//   F wasPressedThisFrame → lock onto target, begin hold timer
//   F isPressed + timer   → tick progress; complete scan at threshold
//   F released early      → interrupt
//
// VFX layer: NOT YET IMPLEMENTED. Console logs prove the loop for MVP.
//
// API confirmed against Unity 6000.4 + Input System 1.x:
//   Keyboard.current — valid when New Input System is active          ✓
//   .wasPressedThisFrame / .isPressed on KeyControl                   ✓
//   Physics2D.Raycast(Vector2, Vector2, float, int) → RaycastHit2D    ✓
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;
using TerrasHeart.Events;

namespace TerrasHeart.Scanner
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ScannerController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Assign ScannerConfig.asset from Assets/Data/ScriptableObjects/Scanner/")]
        [SerializeField] private ScannerConfigSO _config;

        [Header("Scan Origin")]
        [Tooltip("Optional: create an empty child Transform on DrMaria at roughly hand height " +
                 "and assign it here. If left null, the scanner fires from DrMaria's pivot.")]
        [SerializeField] private Transform _scanOrigin;

        // ─── Runtime State ────────────────────────────────────────────────────

        private bool       _isScanHeld;
        private float      _scanProgress;
        private IScannable _currentTarget;
        private bool       _facingRight = true;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_scanOrigin == null)
                _scanOrigin = transform;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            TrackFacingDirection(kb);
            HandleScanInput(kb);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Input Handling
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Tracks which direction Dr. Maria is facing based on A/D input.
        /// Mirrors the same logic in PlayerController so facing is always consistent.
        /// Only updates when a horizontal key is actively pressed — retains last
        /// known direction when no input is held (Maria faces the last way she moved).
        /// </summary>
        private void TrackFacingDirection(Keyboard kb)
        {
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                _facingRight = true;
            else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                _facingRight = false;
        }

        /// <summary>
        /// Handles the full scan input state machine each frame:
        ///   - F just pressed  → attempt target lock, start timer
        ///   - F held          → tick timer, complete on threshold
        ///   - F released      → interrupt if scan was in progress
        /// </summary>
        private void HandleScanInput(Keyboard kb)
        {
            // ── Scan started this frame ───────────────────────────────────────
            if (kb.fKey.wasPressedThisFrame)
            {
                _isScanHeld    = true;
                _scanProgress  = 0f;
                _currentTarget = DetectTarget();

                if (_currentTarget != null)
                {
                    _currentTarget.OnScanBegin();
                    GameEvents.RaiseScanBegin(_currentTarget);

                    string name = _currentTarget.GetData() != null
                        ? _currentTarget.GetData().SpeciesName
                        : "Unknown";

                    Debug.Log($"[Scanner] Locked onto: {name} | Alive: {_currentTarget.IsAlive}");
                }
                else
                {
                    Debug.Log("[Scanner] No scannable target in range.");
                }
            }

            // ── Scan held — tick timer ────────────────────────────────────────
            if (_isScanHeld && kb.fKey.isPressed)
            {
                UpdateScanProgress();
            }

            // ── Scan released before completion ──────────────────────────────
            if (_isScanHeld && kb.fKey.wasReleasedThisFrame)
            {
                if (_currentTarget != null)
                {
                    _currentTarget.OnScanInterrupted();
                    GameEvents.RaiseScanInterrupted();
                    Debug.Log("[Scanner] Scan interrupted — F released early.");
                }

                ResetScanState();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Scan Logic
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Ticks the scan progress timer. Calls CompleteScan() when the threshold
        /// is reached. Resets cleanly if the target disappears mid-scan.
        /// </summary>
        private void UpdateScanProgress()
        {
            if (_currentTarget == null)
            {
                Debug.Log("[Scanner] Target lost mid-scan. Scan reset.");
                ResetScanState();
                return;
            }

            float baseDuration = _config != null ? _config.ScanHoldDuration : 1.5f;
            CreatureDataSO data = _currentTarget.GetData();
            float duration = baseDuration * (data != null ? data.ScanDurationMultiplier : 1f);

            _scanProgress += Time.deltaTime;

            if (_scanProgress >= duration)
                CompleteScan();
        }

        /// <summary>
        /// Called when the hold timer threshold is met.
        /// Builds the ScanResult, notifies the target, and fires the GameEvent.
        /// </summary>
        private void CompleteScan()
        {
            CreatureDataSO data = _currentTarget.GetData();

            if (data == null)
            {
                Debug.LogWarning("[ScannerController] IScannable.GetData() returned null. " +
                                 "Assign a CreatureDataSO to the TestScanTarget component. Scan aborted.");
                ResetScanState();
                return;
            }

            SpecimenTier tier   = data.GetTierFor(_currentTarget.IsAlive);
            ScanResult   result = new ScanResult(data, tier, _currentTarget.IsAlive, Time.time);

            _currentTarget.OnScanComplete();
            GameEvents.RaiseScanComplete(result);

            Debug.Log($"[Scanner] ✓ SCAN COMPLETE ─ {data.SpeciesName} | " +
                      $"Tier: {tier} | Alive: {result.WasAlive} | Biome: {data.BiomeID}");

            ResetScanState();
        }

        /// <summary>
        /// Fires a Physics2D raycast in the direction Dr. Maria is facing.
        /// Returns the first IScannable hit within range, or null.
        /// A debug ray is drawn in the Scene view for testing.
        /// </summary>
        private IScannable DetectTarget()
        {
            if (_config == null)
            {
                Debug.LogWarning("[ScannerController] ScannerConfigSO is not assigned. Cannot scan.");
                return null;
            }

            Vector2 origin    = _scanOrigin.position;
            Vector2 direction = _facingRight ? Vector2.right : Vector2.left;

            RaycastHit2D hit = Physics2D.Raycast(
                origin,
                direction,
                _config.ScanRange,
                _config.ScannableLayer
            );

            // Visible in Scene view — cyan on hit, grey on miss, 0.5s duration
            Debug.DrawRay(
                origin,
                direction * _config.ScanRange,
                hit.collider != null ? Color.cyan : Color.gray,
                0.5f
            );

            if (hit.collider != null)
            {
                IScannable scannable = hit.collider.GetComponent<IScannable>()
                                    ?? hit.collider.GetComponentInParent<IScannable>();

                if (scannable == null)
                    Debug.Log($"[Scanner] Hit '{hit.collider.name}' but no IScannable found. " +
                               "Check: does the object have TestScanTarget? Is it on the Scannable layer?");

                return scannable;
            }

            return null;
        }

        private void ResetScanState()
        {
            _isScanHeld    = false;
            _scanProgress  = 0f;
            _currentTarget = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_config == null || _scanOrigin == null) return;

            UnityEditor.Handles.color = new Color(0f, 1f, 0.83f, 0.35f);
            Vector3 dir = _facingRight ? Vector3.right : Vector3.left;
            UnityEditor.Handles.DrawWireArc(
                _scanOrigin.position,
                Vector3.forward,
                dir,
                30f,
                _config.ScanRange
            );
        }
#endif
    }
}
