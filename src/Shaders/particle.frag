#version 330 core

in vec4 vColor;
out vec4 FragColor;

void main()
{
    // Simple point sprite - circular particle
    // gl_PointCoord is (0,0) to (1,1) within the point
    vec2 coord = gl_PointCoord - vec2(0.5);
    float dist = length(coord);

    // Discard pixels outside circle for round particles
    if (dist > 0.5)
        discard;

    FragColor = vColor;
}
