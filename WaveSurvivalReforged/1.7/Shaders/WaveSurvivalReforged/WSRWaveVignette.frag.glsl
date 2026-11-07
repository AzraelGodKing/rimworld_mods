#version 450

// Wave-incoming vignette: the screen edges glow in the wave's colour. Drawn on one
// quad that covers the camera view. The C# side sets the strength each frame
// (ramp through the warning window, flash on arrival), so the shader has no time input.

#ifdef INSTANCED
#include "InstanceTransforms.glsl"
#endif

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;        // @color @default = vec4(0.8, 0.05, 0.05, 0.0)  rgb = wave colour, a = strength
} uMaterial;

void main()
{
    vec2 d = vUv * 2.0 - 1.0;
    // Squarer than a circle so the long screen edges glow too.
    float r = pow(pow(abs(d.x), 4.0) + pow(abs(d.y), 4.0), 0.25);
    float edge = smoothstep(0.55, 1.05, r);
    outColor = vec4(uMaterial._Color.rgb, edge * uMaterial._Color.a);
}
