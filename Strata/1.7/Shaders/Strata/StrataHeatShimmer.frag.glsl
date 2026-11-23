#version 450

// Heat shimmer near magma, magma vents, geothermal taps and steam geysers.
// _MainTex alpha is how hot each cell is (soft falloff around each source).
// Without a scene grab there is no real refraction: rising, wobbling warm
// bands drawn additively read as hot air over the floor.

#ifdef INSTANCED
#include "InstanceTransforms.glsl"
#endif

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;        // @color @default = vec4(1.0, 0.55, 0.25, 0.22)
    vec4 _MainTex_ST;   // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _StrataParams; // @default = vec4(0.0, 250.0, 250.0, 1.0)  x = time (s), yz = map size (cells)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;

void main()
{
    float t = uMaterial._StrataParams.x;
    vec2 cell = vUv * max(uMaterial._StrataParams.yz, vec2(1.0));
    float heat = texture(_MainTex, vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy).a;

    // Bands rise up the screen (+z), each one wobbling sideways.
    float rise = cell.y * 2.3 - t * 1.6;
    float wobble = sin(cell.x * 3.1 + sin(rise * 0.7) * 0.9 + t * 1.3);
    float band = 0.5 + 0.5 * sin(rise + wobble * 1.2);
    band = band * band * band;
    float flicker = 0.85 + 0.15 * sin(t * 7.0 + cell.x * 0.7 + cell.y * 1.3);

    float strength = heat * band * flicker * uMaterial._Color.a;
    outColor = vec4(uMaterial._Color.rgb * strength, strength);
}
