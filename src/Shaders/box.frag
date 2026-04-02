#version 460 core

in vec3 vPosition;

out vec4 FragColor;

void main()
{
    // Determine if this is a back face based on normal direction
    // Back faces have negative Z in view space
    vec3 normal = normalize(vPosition);

    // Color based on face direction
    vec3 baseColor = vec3(0.2, 0.6, 0.9);

    // Back faces: more transparent
    // Front faces: less transparent
    float alpha = (normal.z < 0.0) ? 0.3 : 0.6;

    FragColor = vec4(baseColor, alpha);
}
