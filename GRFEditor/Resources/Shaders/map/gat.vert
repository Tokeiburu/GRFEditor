#version 330 core

layout (location = 0) in vec3 a_position;
layout (location = 1) in vec2 a_texcoord;
layout (location = 2) in vec3 a_normal;

uniform mat4 modelMatrix = mat4(1.0);
uniform mat4 viewMatrix = mat4(1.0);
uniform mat4 projectionMatrix = mat4(1.0);
uniform float zbias = 0.0f;

out vec3 normal;
out vec2 texCoord;

void main()
{
	mat3 normalMatrix = mat3(modelMatrix * viewMatrix);
	normalMatrix = transpose(inverse(normalMatrix));
	normal = a_normal * normalMatrix;
	
	texCoord = a_texcoord;
	gl_Position = vec4(a_position.x, a_position.y + zbias, a_position.z, 1) * modelMatrix * viewMatrix * projectionMatrix;
}