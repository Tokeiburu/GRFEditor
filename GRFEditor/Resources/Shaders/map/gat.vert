#version 330 core

layout (location = 0) in vec3 a_position;
layout (location = 1) in vec2 a_texcoord;
layout (location = 2) in vec3 a_normal;

uniform mat4 vp;
uniform float zbias = 0.0f;

out vec2 texCoord;

void main()
{
	texCoord = a_texcoord;
	gl_Position = vec4(a_position.x, a_position.y + zbias, a_position.z, 1) * vp;
}