#version 450

// Water caustics on flooded levels. _MainTex alpha marks water cells; the
// pattern is two moving interference waves, drawn additively.

#ifdef INSTANCED
#include "InstanceTransforms.glsl"
#endif

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;        // @color @default = vec4(0.55, 0.75, 0.85, 0.35)
    vec4 _MainTex_ST;   // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _StrataParams; // @default = vec4(0.0, 250.0, 250.0, 1.0)  x = time (s), yz = map size (cells)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;

void main()
{
    float t = uMaterial._StrataParams.x;
    vec2 cell = vUv * max(uMaterial._StrataParams.yz, vec2(1.0));
    float water = texture(_MainTex, vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy).a;

    vec2 p = cell * 1.7;
    float a = sin(p.x + sin(p.y * 0.8 + t * 0.9) * 1.6 + t * 0.7);
    float b = sin(p.y * 1.1 + sin(p.x * 0.6 - t * 0.8) * 1.4 - t * 0.6);
    float light = pow(clamp(1.0 - abs(a + b) * 0.5, 0.0, 1.0), 6.0);

    float strength = light * water * uMaterial._Color.a;
    outColor = vec4(uMaterial._Color.rgb * strength, strength);
}
