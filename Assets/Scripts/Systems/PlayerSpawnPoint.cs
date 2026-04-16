using UnityEngine;

namespace TerrasHeart.Systems
{
    /// <summary>
    /// Place one per scene entry point. On Start, checks if SceneTransitionState
    /// targets this spawn ID and repositions the persistent DrMaria accordingly.
    /// DrMaria is found by "Player" tag — confirm this tag is set on DrMaria root.
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string _spawnID;

        private void Start()
        {
            if (SceneTransitionState.TargetSpawnID != _spawnID) return;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                player.transform.position = transform.position;

            SceneTransitionState.TargetSpawnID = string.Empty;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.3f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, _spawnID);
#endif
        }
    }
}