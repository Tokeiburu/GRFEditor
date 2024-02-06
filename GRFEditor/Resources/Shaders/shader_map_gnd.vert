#version 330 core

layout (location = 0) in vec3 a_position;
layout (location = 1) in vec2 a_texture;
layout (location = 2) in vec2 a_texture2;
layout (location = 3) in vec4 a_color;
layout (location = 4) in vec3 a_normal;

uniform mat4 projectionMatrix;
uniform mat4 cameraMatrix;
uniform mat4 modelMatrix;

out vec2 texCoord;
out vec2 texCoord2;
out vec3 normal;
out vec4 color;

void main(void)
{
	texCoord = a_texture;
	texCoord2 = a_texture2;
	normal = a_normal;
	color = a_color;
	gl_Position = vec4(a_position, 1.0) * modelMatrix * cameraMatrix * projectionMatrix;
}