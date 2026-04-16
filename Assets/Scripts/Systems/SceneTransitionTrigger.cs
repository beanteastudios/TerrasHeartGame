using UnityEngine;
using UnityEngine.SceneManagement;

namespace TerrasHeart.Systems
{
    /// <summary>
    /// Place at scene boundaries. When DrMaria enters the trigger,
    /// stores the target spawn ID and loads the target scene.
    /// DrMaria and GameManagers survive via PersistentEntity.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [Header("Transition")]
        [SerializeField] private string _targetScene;

        [Tooltip("Must match a PlayerSpawnPoint spawnID in the target scene.")]
        [SerializeField] private string _targetSpawnID;

        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            SceneTransitionState.TargetSpawnID = _targetSpawnID;
            SceneManager.LoadScene(_targetScene);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
            Gizmos.DrawCube(transform.position, GetComponent<BoxCollider2D>()
                ? (Vector3)GetComponent<BoxCollider2D>().size : Vector3.one);
        }
    }
}