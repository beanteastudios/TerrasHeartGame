// BioCaveWaterFoam.hlsl
// Custom function nodes for BioCaveWater.shadergraph
// Place at: Assets/Shaders/Water/BioCaveWaterFoam.hlsl
//
// Terra's Heart — Beantea Studios
// Alisavakis sine-modulated shoreline foam + crest foam helpers
// These are called from Custom Function nodes inside the Shader Graph.
// Do not rename functions — the Shader Graph Custom Function nodes reference exact names.

#ifndef BIOCAVEWATER_FOAM_INCLUDED
#define BIOCAVEWATER_FOAM_INCLUDED

// ---------------------------------------------------------------------------
// ShorelineFoam_float
//
// Produces concentric sine-modulated foam rings rolling toward the shoreline.
// Based on Harry Alisavakis' stylized water formula.
//
// depthMask    : 0 at water edge/shore, 1 in deep water (from Scene Depth)
// time         : _Time.y from shader
// scrollSpeed  : how fast rings move toward shore (default ~0.4)
// freq         : density of foam rings (default ~3.0)
// noiseValue   : 0-1 noise sample for organic breakup (Simple Noise, scale ~3)
// out foam     : 0-1 foam contribution (apply FoamColor in graph)
// ---------------------------------------------------------------------------
void ShorelineFoam_float(float depthMask, float time, float scrollSpeed,
                          float freq, float noiseValue, out float foam)
{
    // Edge mask: 1 at shore, 0 deep. Confine foam to shallow band.
    float edgeMask = 1.0 - depthMask;

    // Sine-modulated threshold — produces rolling concentric rings
    float foamInput   = edgeMask * freq - time * scrollSpeed;
    float sineWave    = saturate(sin(foamInput * 6.28318530718)); // 2π
    float threshold   = edgeMask - sineWave * (1.0 - edgeMask);

    // Step against noise for organic breakup, confined to shallow band
    float rawFoam     = step(threshold, noiseValue);
    float shallowBand = step(depthMask, 0.35);  // Only in shallow 35%
    foam = rawFoam * shallowBand * edgeMask;
}

// ---------------------------------------------------------------------------
// CrestFoam_float
//
// Produces foam highlights at ripple crests based on surface normal.
// In orthographic side-view, "crests" are where the perturbed normal
// points most directly toward the camera (+Z in world space).
//
// perturbedNormalZ : Z component of the perturbed normal (from normal maps)
// noiseValue       : 0-1 noise for organic breakup (Simple Noise, small scale)
// power            : sharpness of crest peak (default 40-60)
// out foam         : 0-1 crest foam contribution
// ---------------------------------------------------------------------------
void CrestFoam_float(float perturbedNormalZ, float noiseValue,
                      float power, out float foam)
{
    float facingCamera = saturate(perturbedNormalZ);
    float crestMask    = pow(facingCamera, power);
    foam = crestMask * noiseValue;
}

// ---------------------------------------------------------------------------
// BreathingFactor_float
//
// Subtle sinusoidal breathing modulation for the bioluminescent glow.
// Produces a value that oscillates between minVal and 1.0.
//
// time     : _Time.y
// speed    : oscillation speed (default 0.5)
// minVal   : lower bound (default 0.70)
// out factor : oscillates between minVal and 1.0
// ---------------------------------------------------------------------------
void BreathingFactor_float(float time, float speed, float minVal, out float factor)
{
    float t  = sin(time * speed) * 0.5 + 0.5;  // 0-1
    factor   = lerp(minVal, 1.0, t);
}

#endif // BIOCAVEWATER_FOAM_INCLUDED
