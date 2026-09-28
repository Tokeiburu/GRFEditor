#version 420

layout (location = 0) in vec2 a_position;
layout (location = 1) in vec2 a_texture;

uniform mat4 projectionMatrix;
uniform mat4 cameraMatrix;
uniform mat4 modelMatrix;
uniform vec3 modelPosition;

out vec2 texCoord;

void main()
{
	texCoord = a_texture;
	
	vec2 position = a_position;
	vec4 billboarded = projectionMatrix * cameraMatrix * vec4(modelPosition, 1.0);
	vec4 localPosition = modelMatrix * vec4(position.x, position.y, 0.0, 1.0);
	billboarded.xy += (projectionMatrix * localPosition).xy;
	gl_Position = billboarded;
}