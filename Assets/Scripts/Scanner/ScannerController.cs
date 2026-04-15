// ─────────────────────────────────────────────────────────────────────────────
// ScannerController.cs
// Path: Assets/Scripts/Scanner/ScannerController.cs
// Terra's Heart — Dr. Maria's scanner. The primary verb of the entire game.
//
// Updated for Step 4: reads scan duration multiplier from CrewManager.
// If CrewManager is null or unassigned, base duration applies — no regression.
//
// Effective scan duration = base × creature multiplier × crew multiplier
// Example: 1.5s × 1.0 (creature) × 0.6 (Yuki on Research) = 0.9s
//
// Input: Keyboard.current polling — Scan key: F
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;
using TerrasHeart.Events;
using TerrasHeart.Crew;

namespace TerrasHeart.Scanner
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ScannerController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Assign ScannerConfig.asset from Assets/Data/ScriptableObjects/Scanner/")]
        [SerializeField] private ScannerConfigSO _config;

        [Header("References")]
        [Tooltip("Optional scan origin transform at chest height on DrMaria. " +
                 "Falls back to DrMaria pivot if left empty.")]
        [SerializeField] private Transform _scanOrigin;

        [Tooltip("Optional. If assigned, scan hold duration is modified by crew assignment bonuses. " +
                 "Assign the CrewManager component from GameManagers.")]
        [SerializeField] private CrewManager _crewManager;

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

        private void TrackFacingDirection(Keyboard kb)
        {
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                _facingRight = true;
            else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                _facingRight = false;
        }

        private void HandleScanInput(Keyboard kb)
        {
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

            if (_isScanHeld && kb.fKey.isPressed)
                UpdateScanProgress();

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
            float creatureMultiplier = data != null ? data.ScanDurationMultiplier : 1f;

            // Crew multiplier — 1.0 if CrewManager not assigned (no regression)
            float crewMultiplier = _crewManager != null
                ? _crewManager.GetScanDurationMultiplier()
                : 1f;

            float effectiveDuration = baseDuration * creatureMultiplier * crewMultiplier;

            _scanProgress += Time.deltaTime;

            if (_scanProgress >= effectiveDuration)
                CompleteScan();
        }

        private void CompleteScan()
        {
            CreatureDataSO data = _currentTarget.GetData();

            if (data == null)
            {
                Debug.LogWarning("[ScannerController] IScannable.GetData() returned null. Scan aborted.");
                ResetScanState();
                return;
            }

            SpecimenTier tier   = data.GetTierFor(_currentTarget.IsAlive);
            ScanResult   result = new ScanResult(data, tier, _currentTarget.IsAlive, Time.time);

            _currentTarget.OnScanComplete();
            GameEvents.RaiseScanComplete(result);

            // Log effective duration for crew bonus verification
            float baseDuration       = _config != null ? _config.ScanHoldDuration : 1.5f;
            float crewMultiplier     = _crewManager != null ? _crewManager.GetScanDurationMultiplier() : 1f;
            float effectiveDuration  = baseDuration * data.ScanDurationMultiplier * crewMultiplier;

            Debug.Log($"[Scanner] ✓ SCAN COMPLETE ─ {data.SpeciesName} | " +
                      $"Tier: {tier} | Alive: {result.WasAlive} | " +
                      $"Duration: {effectiveDuration:F2}s (crew ×{crewMultiplier:F2})");

            ResetScanState();
        }

        private IScannable DetectTarget()
        {
            if (_config == null)
            {
                Debug.LogWarning("[ScannerController] ScannerConfigSO not assigned.");
                return null;
            }

            Vector2 origin    = _scanOrigin.position;
            Vector2 direction = _facingRight ? Vector2.right : Vector2.left;

            RaycastHit2D hit = Physics2D.Raycast(
                origin, direction, _config.ScanRange, _config.ScannableLayer);

            Debug.DrawRay(origin, direction * _config.ScanRange,
                hit.collider != null ? Color.cyan : Color.gray, 0.5f);

            if (hit.collider != null)
            {
                IScannable scannable = hit.collider.GetComponent<IScannable>()
                                    ?? hit.collider.GetComponentInParent<IScannable>();

                if (scannable == null)
                    Debug.Log($"[Scanner] Hit '{hit.collider.name}' but no IScannable found.");

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
            UnityEditor.Handles.DrawWireArc(_scanOrigin.position, Vector3.forward, dir, 30f, _config.ScanRange);
        }
#endif
    }
}
