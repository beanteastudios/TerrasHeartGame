// ─────────────────────────────────────────────────────────────────────────────
// GlowMantleConfigSO.cs
// Path: Assets/Scripts/Creatures/GlowMantleConfigSO.cs
// Terra's Heart — Data config for the Glow-Mantle call-and-response mechanic.
//
// Create via: TerrasHeart → Creatures → GlowMantleConfig
// Assign to GlowMantleAI on the Glow-Mantle GameObject.
//
// Sequence values: 0 = Cyan (#00FFD1), 1 = Amber (#FFB347).
// Phase A: Sequence = {0, 1}. Extend for later biome variants — no code changes.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace TerrasHeart.Creatures
{
    [CreateAssetMenu(fileName = "NewGlowMantleConfig",
                     menuName = "TerrasHeart/Creatures/GlowMantleConfig")]
    public class GlowMantleConfigSO : ScriptableObject
    {
        [Header("Pattern Sequence")]
        [Tooltip("0 = Cyan, 1 = Amber. Phase A: {0, 1}. " +
                 "Add elements for harder variants in later biomes.")]
        public int[] Sequence = { 0, 1 };

        [Header("Broadcast Timing")]
        [Tooltip("How long each colour pulse shows on the creature's skin.")]
        public float PulseDuration = 0.65f;

        [Tooltip("Gap between pulses.")]
        public float PauseBetweenPulses = 0.5f;

        [Tooltip("Pause after the last pulse before the input window opens. " +
                 "Gives the player a moment to process the sequence.")]
        public float PostSequencePause = 0.8f;

        [Header("Input Window")]
        [Tooltip("Seconds the player has to press each key in the sequence. " +
                 "Timer resets on each correct input.")]
        public float InputTimePerPulse = 2.0f;

        [Header("Interaction Range")]
        [Tooltip("Player must be within this distance to trigger call-and-response. " +
                 "Skin adaptation must be active. Recommended: slightly inside aggro radius.")]
        public float CallAndResponseRange = 5f;

        [Header("Colours")]
        [Tooltip("Cyan — swatch 1 and Sequence value 0.")]
        public Color CyanColour = new Color(0f, 1f, 0.82f, 1f);

        [Tooltip("Amber — swatch 2 and Sequence value 1.")]
        public Color AmberColour = new Color(1f, 0.70f, 0.28f, 1f);

        [Header("State Durations")]
        [Tooltip("Seconds the scan window stays open after a correct match. " +
                 "Must exceed ScannerConfig.ScanHoldDuration to guarantee a full scan.")]
        public float ReceptiveDuration = 5f;

        [Tooltip("Seconds the creature stays in cold-dismissed state before returning " +
                 "to patrol. Long enough to feel like a consequence.")]
        public float DismissedDuration = 2.5f;
    }
}