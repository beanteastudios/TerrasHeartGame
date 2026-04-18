// Path: Assets/Scripts/Systems/CameraTargetAssigner.cs
using UnityEngine;
using Unity.Cinemachine;

namespace TerrasHeart.Systems
{
    /// <summary>
    /// Finds the persistent DrMaria by tag at scene load and assigns
    /// her as the Follow/LookAt target for the scene's virtual camera.
    /// Attach to the PlayerFollowCam GameObject in every non-Prologue scene.
    /// </summary>
    public class CameraTargetAssigner : MonoBehaviour
    {
        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[CameraTargetAssigner] Player not found.");
                return;
            }

            CinemachineCamera vcam = GetComponent<CinemachineCamera>();
            if (vcam == null)
            {
                Debug.LogWarning("[CameraTargetAssigner] No CinemachineCamera " +
                                 "on this GameObject.");
                return;
            }

            vcam.Target.TrackingTarget = player.transform;
            vcam.Target.LookAtTarget = player.transform;

            Debug.Log("[CameraTargetAssigner] Camera target assigned to DrMaria.");
        }
    }
}