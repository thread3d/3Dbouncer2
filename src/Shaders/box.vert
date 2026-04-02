#version 460 core

layout(location = 0) in vec3 aPosition;

uniform mat4 mvp;

out vec3 vPosition;

void main()
{
    vPosition = aPosition;
    gl_Position = mvp * vec4(aPosition, 1.0);
}
