#version 330 core

layout (location = 0) in vec3 a_position;
layout (location = 1) in vec2 a_texture;
layout (location = 2) in vec2 a_texture2;
layout (location = 3) in vec4 a_color;
layout (location = 4) in vec3 a_normal;

uniform mat4 mvp;
uniform vec3 lightDiffuse;
uniform vec3 lightAmbient;
uniform vec3 lightDirection;
uniform bool wireframe;

out vec2 texCoord;
out vec2 texCoord2;
out vec3 normal;
out vec3 FragPos;
out vec4 color;
out vec3 mult;

void main(void)
{
	texCoord = a_texture;
	texCoord2 = a_texture2;
	normal = a_normal;
	color = a_color;
	gl_Position = vec4(a_position, 1.0) * mvp;
	
	if (wireframe) {
		FragPos = a_position;
	}
	
	// The light calculation is performed here for linear color interpolation,
	// which is what the official client does.
	float NL = clamp(dot(normalize(normal), -1.0f * lightDirection),0.0,1.0);
	vec3 ambientFactor = (1.0 - lightAmbient) * lightAmbient;
	vec3 ambient = lightAmbient - ambientFactor + ambientFactor * lightDiffuse;
	vec3 diffuseFactor = (1.0 - lightDiffuse) * lightDiffuse;
	vec3 diffuse = lightDiffuse - diffuseFactor + diffuseFactor * lightAmbient;
	vec3 mult1 = min(NL * diffuse + ambient, 1.0);
	// The formula quite literally changes when the combined value of lightAmbient + lightDiffuse is greater than 1.0
	vec3 mult2 = min(max(lightDiffuse, lightAmbient) + (1.0 - max(lightDiffuse, lightAmbient)) * min(lightDiffuse, lightAmbient), 1.0);
	mult = clamp(min(mult1, mult2), 0.0, 1.0);
}