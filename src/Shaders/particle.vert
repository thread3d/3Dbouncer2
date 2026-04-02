#version 430 core

layout(location = 0) in vec2 aVertex;  // Base quad vertex (-0.5 to 0.5)

// SSBO with particle data - std430 layout
// Each particle: Position (vec4) + Velocity (vec4) + Color (vec4) = 48 bytes
layout(std430, binding = 0) buffer ParticleData {
    vec4 data[];  // Position at data[i*3], Color at data[i*3+2]
} particles;

uniform mat4 mvp;
uniform float uParticleSize;

out vec4 vColor;
out vec2 vTexCoord;

void main()
{
    // Get particle index from instance ID
    // Each particle: Position (vec4) + Velocity (vec4) + Color (vec4) = 3 vec4s
    uint idx = uint(gl_InstanceID) * 3u;

    // Extract position and color from SSBO
    vec3 position = particles.data[idx].xyz;
    vColor = particles.data[idx + 2u];  // Skip Velocity at idx+1

    // Pass texture coordinate for circular particle in fragment shader
    vTexCoord = aVertex + 0.5;

    // Expand vertex by particle size
    vec2 offset = aVertex * uParticleSize * 0.01;

    // Apply MVP transform
    gl_Position = mvp * vec4(position + vec3(offset, 0.0), 1.0);
}
