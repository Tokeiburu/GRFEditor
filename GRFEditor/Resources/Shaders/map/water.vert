#version 330 core

layout (location = 0) in vec3 a_position;
layout (location = 1) in vec2 a_texcoord;

uniform mat4 mvp;

uniform float waterHeight;
uniform float amplitude;
uniform float waveSpeed;
uniform float wavePitch;
uniform float time;

out vec2 texCoord;

void main()
{
	texCoord = a_texcoord;

	float height = waterHeight;
	height += amplitude*cos(radians((waveSpeed*50*time)+(a_position.x-a_position.z)*.1*wavePitch));
	
	gl_Position =  vec4(vec3(a_position.x, height, a_position.z),1) * mvp;
}