// ─────────────────────────────────────────────────────────────────────────────
// ThrowConfigSO.cs
// Path: Assets/Scripts/Combat/ThrowConfigSO.cs
// Terra's Heart — Data config for the throw system.
//
// Create via: TerrasHeart → Combat → ThrowConfig
// Assign to ThrowController on DrMaria.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Environment;

namespace TerrasHeart.Combat
{
    [CreateAssetMenu(fileName = "NewThrowConfig", menuName = "TerrasHeart/Combat/ThrowConfig")]
    public class ThrowConfigSO : ScriptableObject
    {
        [Header("Throw Physics")]
        [Tooltip("Horizontal distance the food travels from the throw origin.")]
        [SerializeField] public float ThrowRange = 4f;

        [Header("Food Marker")]
        [Tooltip("How long the food marker stays active before auto-destroying. " +
                 "Long enough for a creature to react and approach.")]
        [SerializeField] public float MarkerLifetime = 6f;

        [Header("Cost")]
        [Tooltip("The crafting material type consumed per throw. " +
                 "Biological = Bioluminescent Moss for cave creatures.")]
        [SerializeField] public ResourceNodeType FoodMaterialType = ResourceNodeType.Biological;

        [Tooltip("Units of FoodMaterialType consumed per throw.")]
        [SerializeField] public int FoodCostPerThrow = 1;
    }
}