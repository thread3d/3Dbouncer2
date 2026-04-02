#version 430 core

in vec4 vColor;
in vec2 vTexCoord;
out vec4 FragColor;

void main()
{
    // Calculate distance from center of particle (vTexCoord is 0-1 range)
    vec2 coord = vTexCoord - vec2(0.5);
    float dist = length(coord);

    // Discard pixels outside circle for round particles
    if (dist > 0.5)
        discard;

    // Optional: smooth edge for nicer appearance
    float alpha = smoothstep(0.5, 0.45, dist);

    FragColor = vec4(vColor.rgb, vColor.a * alpha);
}
