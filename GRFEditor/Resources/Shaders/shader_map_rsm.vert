#version 330 core

layout (location = 0) in vec3 a_position;
layout (location = 1) in vec2 a_texture;
layout (location = 2) in vec3 a_normal;
layout (location = 3) in float a_cull;

uniform mat4 projectionMatrix;
uniform mat4 cameraMatrix;
uniform mat4 modelMatrix;
uniform mat4 modelMatrix2;

out vec2 texCoord;
out vec3 normal;
out vec3 FragPos;
out float cull;

void main(void)
{
	mat3 normalMatrix = mat3(modelMatrix * modelMatrix2);
	normal = a_normal * normalMatrix;
	normal = normalize(normal);
	cull = a_cull;
	
	texCoord = a_texture;
	gl_Position = vec4(a_position, 1.0) * modelMatrix * modelMatrix2 * cameraMatrix * projectionMatrix;
	FragPos = vec3(vec4(a_position, 1.0) * modelMatrix * modelMatrix2);
}