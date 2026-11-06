#version 450

// Screen-edge darkening for underground levels, stronger the deeper the
// level. Drawn on one quad that covers the camera view.

#ifdef INSTANCED
#include "InstanceTransforms.glsl"
#endif

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;        // @color @default = vec4(0.0, 0.0, 0.0, 0.5)  rgb = tint, a = strength
} uMaterial;

void main()
{
    vec2 d = vUv * 2.0 - 1.0;
    float r = length(d) * 0.7071;
    float edge = smoothstep(0.45, 1.0, r);
    outColor = vec4(uMaterial._Color.rgb, edge * uMaterial._Color.a);
}
