using UnityEngine;

namespace TerrasHeart.Environment
{
    [CreateAssetMenu(fileName = "StalactiteConfig_", menuName = "TerrasHeart/Environment/Stalactite Config")]
    public class StalactiteConfigSO : ScriptableObject
    {
        [Header("Trigger")]
        [Tooltip("Radius of proximity check below the stalactite. Player entering this triggers the warning.")]
        public float ProximityRadius = 2f;

        [Header("Warning")]
        [Tooltip("Duration of the visual shake warning before the stalactite falls (seconds).")]
        public float WarningDuration = 1.5f;

        [Header("Fall")]
        [Tooltip("Fall speed (units/s). Controlled — not gravity-based.")]
        public float FallSpeed = 12f;

        [Header("Damage")]
        [Tooltip("HP damage dealt on player contact. Not instakill by default.")]
        public int Damage = 20;

        [Tooltip("If true, this stalactite deals 9999 damage (effectively instakill). Use a visually distinct prefab. Introduce only after standard stalactites are understood.")]
        public bool IsInstakill = false;

        [Header("Ecological Consequence")]
        [Tooltip("Biome health penalty applied when this stalactite lands. Use the BiomeID matching the scene (e.g. 'BrineglowDescent'). Positive value — negated internally.")]
        public float BiomeHealthPenalty = 2f;

        [Tooltip("BiomeID matching the BiomeHealthConfigSO for this scene.")]
        public string BiomeID = "BrineglowDescent";
    }
}
