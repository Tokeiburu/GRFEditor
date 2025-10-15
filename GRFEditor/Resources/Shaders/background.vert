#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

out vec2 texCoord;

uniform vec2 uViewportSize;
uniform float uTexSize;

void main(void)
{
	vec2 screenPos = vec2(
		(aPosition.x * 0.5 + 0.5) * uViewportSize.x,
		(1.0 - (aPosition.y * 0.5 + 0.5)) * uViewportSize.y
	);
	texCoord = screenPos / uTexSize;
	
    gl_Position = vec4(aPosition, 1.0);
}