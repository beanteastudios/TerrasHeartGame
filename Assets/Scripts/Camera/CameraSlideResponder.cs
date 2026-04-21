// ─────────────────────────────────────────────────────────────────────────────
// CameraSlideResponder.cs
// Path: Assets/Scripts/Camera/CameraSlideResponder.cs
// Terra's Heart — Zeroes PlayerFollowCam damping during the slide state and
// any subsequent airborne phase, then restores normal damping only once
// Dr. Maria is back on the ground.
//
// Attach to: PlayerFollowCam (alongside CinemachineCamera and
//            CinemachinePositionComposer).
//
// Inspector setup:
//   - Player Controller → drag DrMaria (PlayerController component)
//
// Usage:
//   Set your preferred normal-movement damping values directly on the
//   CinemachinePositionComposer in the Inspector. This script reads those
//   values on Awake and treats them as the restore target automatically.
//
// ⚠ Namespace is TerrasHeart.Cameras (not TerrasHeart.Camera) to avoid
//   collision with UnityEngine.Camera in scripts that don't fully qualify it.
// ⚠ Cinemachine 3.x API (Unity.Cinemachine namespace).
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using TerrasHeart.Events;
using TerrasHeart.Player;

namespace TerrasHeart.Cameras
{
    [RequireComponent(typeof(CinemachinePositionComposer))]
    public class CameraSlideResponder : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Drag DrMaria here — used to detect when she lands after the slide.")]
        [SerializeField] private PlayerController _playerController;

        // Captured on Awake from the Inspector values on CinemachinePositionComposer.
        // Whatever damping is set there becomes the restore target automatically.
        private CinemachinePositionComposer _composer;
        private Vector3 _normalDamping;

        private Coroutine _restoreRoutine;

        // ─── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _composer = GetComponent<CinemachinePositionComposer>();
            _normalDamping = _composer.Damping;
        }

        private void OnEnable()
        {
            GameEvents.OnSlideStateChanged += HandleSlideStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnSlideStateChanged -= HandleSlideStateChanged;
        }

        // ─── Slide Response ───────────────────────────────────────────────────

        private void HandleSlideStateChanged(bool isSliding)
        {
            if (isSliding)
            {
                // Cancel any pending restore — she's sliding again.
                if (_restoreRoutine != null)
                {
                    StopCoroutine(_restoreRoutine);
                    _restoreRoutine = null;
                }

                _composer.Damping = Vector3.zero;
            }
            else
            {
                // Slide exited but she may be airborne off the ramp.
                // Wait until she's actually grounded before restoring damping.
                if (_restoreRoutine != null)
                    StopCoroutine(_restoreRoutine);

                _restoreRoutine = StartCoroutine(RestoreOnLanding());
            }
        }

        private IEnumerator RestoreOnLanding()
        {
            // Poll IsGrounded every frame until she touches down.
            while (_playerController != null && !_playerController.IsGrounded)
                yield return null;

            _composer.Damping = _normalDamping;
            _restoreRoutine = null;
        }
    }
}