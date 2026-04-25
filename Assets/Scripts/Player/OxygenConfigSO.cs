using UnityEngine;

namespace TerrasHeart.Player
{
    [CreateAssetMenu(fileName = "OxygenConfig", menuName = "TerrasHeart/Player/Oxygen Config")]
    public class OxygenConfigSO : ScriptableObject
    {
        [Header("Oxygen Pool")]
        [Tooltip("Maximum oxygen units. Depletes to 0, refills to this value.")]
        public float MaxOxygen = 100f;

        [Header("Drain & Refill")]
        [Tooltip("Oxygen drained per second while underwater (units/s).")]
        public float DrainRate = 10f;

        [Tooltip("Oxygen refilled per second at the surface or out of water (units/s). Should feel faster than drain — surface is relief.")]
        public float RefillRate = 30f;

        [Header("Thresholds")]
        [Tooltip("Oxygen level at which OnOxygenCritical fires and vignette begins pulsing.")]
        public float CriticalThreshold = 20f;

        [Header("Consequence")]
        [Tooltip("Health drained per second once oxygen hits 0. Not instant death — gives player a few seconds. Stubbed until HealthManager exists.")]
        public float HealthDrainRate = 5f;
    }
}
