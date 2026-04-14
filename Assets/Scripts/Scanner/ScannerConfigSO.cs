// ─────────────────────────────────────────────────────────────────────────────
// ScannerConfigSO.cs
// Path: Assets/Scripts/Scanner/ScannerConfigSO.cs
// Terra's Heart — ScriptableObject config for Dr. Maria's scanner.
//
// Create via: Assets > Create > TerrasHeart > Scanner > ScannerConfig
// Save to:    Assets/Data/ScriptableObjects/Scanner/ScannerConfig.asset
//
// One instance exists for the base scanner. Future upgrades (via Adaptation system)
// may swap or modify this config to extend range, reduce hold duration, etc.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace TerrasHeart.Scanner
{
    [CreateAssetMenu(fileName = "ScannerConfig", menuName = "TerrasHeart/Scanner/ScannerConfig")]
    public class ScannerConfigSO : ScriptableObject
    {
        [Header("Beam")]
        [Tooltip("Maximum world-space distance the scanner raycast can reach. " +
                 "At orthographic size 5, the half-screen width is ~8.9 units. " +
                 "Default 5 keeps scans within comfortable reach without trivialising distance.")]
        [SerializeField] private float _scanRange = 5f;

        [Tooltip("Time in seconds the player must hold the scan button to complete a scan. " +
                 "1.5s is the baseline — creatures with high ScanDurationMultiplier take longer " +
                 "(rare/elusive species reward patience).")]
        [SerializeField] private float _scanHoldDuration = 1.5f;

        [Header("Detection")]
        [Tooltip("Physics2D layer mask for scannable objects. " +
                 "Create a 'Scannable' layer in Project Settings > Tags and Layers, " +
                 "then assign it here. All IScannable objects must be on this layer.")]
        [SerializeField] private LayerMask _scannableLayer;

        // ─── Public API (read-only properties — no external mutation) ─────────

        public float ScanRange        => _scanRange;
        public float ScanHoldDuration => _scanHoldDuration;
        public LayerMask ScannableLayer => _scannableLayer;
    }
}
