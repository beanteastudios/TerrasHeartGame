// ─────────────────────────────────────────────────────────────────────────────
// FoodMarker.cs
// Path: Assets/Scripts/Environment/FoodMarker.cs
// Terra's Heart — World object placed by ThrowController after a food throw.
//
// Attach to: FoodMarker prefab (Assets/Prefabs/Environment/FoodMarker.prefab)
//
// On Start, raises GameEvents.OnFoodPlaced(worldPosition).
// Creature AI scripts subscribe and check their distance from this position:
//   < frightenRadius  → creature flees (food landed too close)
//   < comfortRange    → creature enters feeding / comfort state
//   > comfortRange    → food placed too far, creature ignores it
//
// Lifecycle is managed by ThrowController — Destroy is scheduled there
// via Destroy(marker, config.MarkerLifetime). This script does not
// self-destruct.
//
// The CircleCollider2D trigger on this prefab is used by creature AI scripts
// via OnTriggerEnter2D to detect when they physically reach the food.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.Environment
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class FoodMarker : MonoBehaviour
    {
        private void Start()
        {
            GameEvents.RaiseFoodPlaced(transform.position);
            Debug.Log($"[FoodMarker] Placed at {(Vector2)transform.position}");
        }
    }
}