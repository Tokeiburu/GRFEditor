#version 330 core

layout (location = 0) in vec3 a_position;

uniform mat4 modelMatrix = mat4(1.0);
uniform mat4 viewMatrix = mat4(1.0);
uniform mat4 projectionMatrix = mat4(1.0);

void main()
{
	gl_Position = vec4(a_position,1) * modelMatrix * viewMatrix * projectionMatrix;
}