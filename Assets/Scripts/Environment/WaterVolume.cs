using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.Environment
{
    /// <summary>
    /// Attach to a GameObject with a BoxCollider2D (Is Trigger = true).
    /// Raises GameEvents.OnWaterEntered / OnWaterExited when DrMaria enters or exits the volume.
    /// PlayerController never subscribes to trigger callbacks directly — it uses the GameEvents bus.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WaterVolume : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("Per-water-body config. Assign a WaterVolumeSO for this pool.")]
        [SerializeField] private WaterVolumeSO _volumeConfig;

        private void Reset()
        {
            if (TryGetComponent<Collider2D>(out var col))
                col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[WaterVolume] Trigger entered by: {other.gameObject.name} tag: {other.gameObject.tag}");

            if (!other.CompareTag("Player")) return;

            if (_volumeConfig == null)
            {
                Debug.LogWarning($"[WaterVolume] No WaterVolumeSO assigned on {gameObject.name}. Defaulting SurfaceY to 0.", this);
                GameEvents.RaiseWaterEntered(transform.position.y);
                return;
            }

            GameEvents.RaiseWaterEntered(_volumeConfig.SurfaceY);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            GameEvents.RaiseWaterExited();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_volumeConfig == null) return;
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.25f);
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 5f, _volumeConfig.SurfaceY, 0f),
                new Vector3(transform.position.x + 5f, _volumeConfig.SurfaceY, 0f));
        }
#endif
    }
}