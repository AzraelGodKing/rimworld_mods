#version 450

// Gas overlay. _MainTex holds one texel per map cell with the cell's gas
// colour (alpha = how strong). Bilinear sampling blends neighbouring cells,
// and a slow drift in the sample position plus a soft density ripple make
// the gas move instead of sitting in squares.

#ifdef INSTANCED
#include "InstanceTransforms.glsl"
#endif

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;        // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST;   // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _StrataParams; // @default = vec4(0.0, 250.0, 250.0, 1.0)  x = time (s), yz = map size (cells), w = motion
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;

void main()
{
    float t = uMaterial._StrataParams.x;
    vec2 mapSize = max(uMaterial._StrataParams.yz, vec2(1.0));
    float motion = uMaterial._StrataParams.w;
    vec2 cell = vUv * mapSize;

    // Drift of up to about a third of a cell, slow and low frequency.
    vec2 drift = vec2(
        sin(cell.y * 0.35 + t * 0.6) + sin(cell.x * 0.17 - t * 0.35),
        cos(cell.x * 0.31 + t * 0.5) + cos(cell.y * 0.21 + t * 0.4)) * 0.17 * motion;
    vec2 uv = (cell + drift) / mapSize;

    vec4 gas = texture(_MainTex, uv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy);
    float ripple = 1.0 + 0.18 * motion * sin(cell.x * 0.9 + cell.y * 0.7 + t * 1.3);
    outColor = vec4(gas.rgb, clamp(gas.a * ripple, 0.0, 1.0)) * uMaterial._Color;
}
