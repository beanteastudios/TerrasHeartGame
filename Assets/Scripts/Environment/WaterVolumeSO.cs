using UnityEngine;

namespace TerrasHeart.Environment
{
    [CreateAssetMenu(fileName = "WaterVolume_", menuName = "TerrasHeart/Environment/Water Volume Config")]
    public class WaterVolumeSO : ScriptableObject
    {
        [Tooltip("World-space Y position of the water surface. Set this to match the visual surface in the Scene view.")]
        public float SurfaceY = 0f;

        [Tooltip("Multiplier applied to OxygenConfigSO.DrainRate for this water body. 1 = normal, 2 = twice as toxic. Reserved for deeper biomes.")]
        [Range(0.1f, 5f)]
        public float OxygenDrainMultiplier = 1f;
    }
}
