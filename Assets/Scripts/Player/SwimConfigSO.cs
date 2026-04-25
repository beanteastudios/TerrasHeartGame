using UnityEngine;

namespace TerrasHeart.Player
{
    [CreateAssetMenu(fileName = "SwimConfig", menuName = "TerrasHeart/Player/Swim Config")]
    public class SwimConfigSO : ScriptableObject
    {
        [Header("Swim Speeds")]
        [Tooltip("Horizontal swim speed (units/s). Land move speed is 5 — this should feel slower.")]
        public float HorizontalSpeed = 3f;

        [Tooltip("Upward swim speed (units/s) when W is held.")]
        public float UpSpeed = 2.5f;

        [Tooltip("Downward swim speed (units/s) when S is held. Slightly faster — gravity assist feel.")]
        public float DownSpeed = 3.5f;

        [Tooltip("Gentle upward drift (units/s) when no vertical input. Player floats up naturally.")]
        public float BuoyancyForce = 0.5f;
    }
}
