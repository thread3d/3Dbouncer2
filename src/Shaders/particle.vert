#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec4 aColor;

uniform mat4 mvp;
uniform float uPointSize;

out vec4 vColor;

void main()
{
    gl_Position = mvp * vec4(aPosition, 1.0);
    gl_PointSize = uPointSize;
    vColor = aColor;
}
