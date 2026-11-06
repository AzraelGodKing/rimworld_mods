#version 450

// See-below hole mask. _MainTex holds one texel per map cell: alpha is how
// much to darken the view of the level below in that cell. Bilinear sampling
// softens the hole edges that the 1.6 per-cell quads drew hard.

#ifdef INSTANCED
#include "InstanceTransforms.glsl"
#endif

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;      // @color @default = vec4(0.0, 0.0, 0.0, 1.0)
    vec4 _MainTex_ST; // @default = vec4(0.0, 0.0, 1.0, 1.0)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;

void main()
{
    float dim = texture(_MainTex, vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy).a;
    outColor = vec4(uMaterial._Color.rgb, dim * uMaterial._Color.a);
}
