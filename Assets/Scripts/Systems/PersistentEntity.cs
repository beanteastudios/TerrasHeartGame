using System.Collections.Generic;
using UnityEngine;

namespace TerrasHeart.Systems
{
    /// <summary>
    /// Add to any root GameObject that must survive scene loads.
    /// Attach to DrMaria root and GameManagers root in PrologueMeridianOcean.
    /// Prevents duplicates if the scene is re-entered during testing.
    /// </summary>
    public class PersistentEntity : MonoBehaviour
    {
        private static readonly Dictionary<string, PersistentEntity> _instances
            = new Dictionary<string, PersistentEntity>();

        private void Awake()
        {
            string key = gameObject.name;

            if (_instances.TryGetValue(key, out PersistentEntity existing) && existing != null)
            {
                Destroy(gameObject);
                return;
            }

            _instances[key] = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}