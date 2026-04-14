// ─────────────────────────────────────────────────────────────────────────────
// AdaptationSO.cs
// Path: Assets/Scripts/Adaptations/AdaptationSO.cs
// Terra's Heart — Definition of one adaptation Dr. Maria can unlock.
// One SO per adaptation. All adaptations are derived from scanned creature data
// and crafted by crew — they are biological, not technological.
//
// Create via: Assets > Create > TerrasHeart > Adaptations > Adaptation
// Save to:    Assets/Data/ScriptableObjects/Adaptations/
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace TerrasHeart.Adaptations
{
    [CreateAssetMenu(fileName = "NewAdaptation", menuName = "TerrasHeart/Adaptations/Adaptation")]
    public class AdaptationSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name shown in the adaptation UI. Example: 'Enhanced Legs'")]
        [SerializeField] private string _displayName;

        [Tooltip("Which of Dr. Maria's six biological slots this adaptation occupies.")]
        [SerializeField] private AdaptationSlot _slot;

        [Header("Description")]
        [Tooltip("Flavour text describing the biological adaptation in Dr. Maria's voice.")]
        [TextArea(2, 5)]
        [SerializeField] private string _description;

        [Header("Effect")]
        [Tooltip("What this adaptation does to Dr. Maria's capabilities.")]
        [SerializeField] private AdaptationEffectType _effectType;

        [Tooltip("Numeric value for the effect. For bool effects (immunity, grapple) this is ignored. " +
                 "For JumpForceBonus: adds directly to jumpForce. " +
                 "For MoveSpeedBonus: adds directly to moveSpeed.")]
        [SerializeField] private float _effectValue;

        // ─── Public API ───────────────────────────────────────────────────────

        public string DisplayName          => _displayName;
        public AdaptationSlot Slot         => _slot;
        public string Description          => _description;
        public AdaptationEffectType EffectType => _effectType;
        public float EffectValue           => _effectValue;
    }
}
