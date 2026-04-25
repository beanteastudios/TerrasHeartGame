using UnityEngine;

namespace TerrasHeart.Environment
{
    [CreateAssetMenu(fileName = "JumpPadConfig_", menuName = "TerrasHeart/Environment/Jump Pad Config")]
    public class JumpPadConfigSO : ScriptableObject
    {
        [Header("Launch")]
        [Tooltip("Vertical launch force (units/s). Must exceed Legs adaptation jump (15) — 18 is clearly special.")]
        public float LaunchForce = 18f;

        [Header("Cooldown")]
        [Tooltip("Seconds before pad can fire again after a launch. Prevents rapid double-launches.")]
        public float Cooldown = 1.5f;
    }
}
