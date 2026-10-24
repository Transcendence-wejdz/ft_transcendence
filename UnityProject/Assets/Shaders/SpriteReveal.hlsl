// Shared see-through circle for the reveal sprite shaders; each pass declares _RevealOpen in its UnityPerMaterial buffer.
#ifndef SPRITE_REVEAL_INCLUDED
#define SPRITE_REVEAL_INCLUDED

float4 _RevealCenter;
float _RevealRadius;
float _RevealAlpha;
float _RevealSoftness;
float _RevealPixelsPerUnit;

// Opacity multiplier: _RevealAlpha inside the circle around the local player; across the rim a 4x4 Bayer pattern, snapped to the sprite pixel grid, stipples it back to opaque.
half RevealFactor(float2 positionWS)
{
    float radius = max(_RevealRadius * _RevealOpen, 0.0001);
    float inner = radius * (1.0 - _RevealSoftness);
    float2 pixel = floor(positionWS * max(_RevealPixelsPerUnit, 1.0));
    float2 snapped = (pixel + 0.5) / max(_RevealPixelsPerUnit, 1.0);
    float t = saturate((distance(snapped, _RevealCenter.xy) - inner) / max(radius - inner, 0.0001));
    const float bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
    uint2 cell = (uint2)fmod(fmod(pixel, 4.0) + 4.0, 4.0);
    float threshold = (bayer[cell.y * 4 + cell.x] + 0.5) / 16.0;
    return t > threshold ? 1.0 : _RevealAlpha;
}

#endif
