#version 330 core

layout (location = 0) in vec2 a_position;
layout (location = 1) in float a_alpha;
layout (location = 2) in vec3 pos;

uniform mat4 view;
uniform mat4 vp;
uniform mat4 m;
uniform float billboard_off;
uniform vec2 scale;

out vec2 texCoord;
out float alpha;

void main(void)
{
	vec2 position = a_position * scale;
	texCoord = vec2(a_position.x < 0 ? 0 : 1, a_position.y < 0 ? 0 : 1);
	alpha = a_alpha;
	
	if (billboard_off == 0) {
		vec3 camRight = vec3(view[0][0], view[0][1], view[0][2]);
		vec3 camUp    = vec3(view[1][0], view[1][1], view[1][2]);
		vec3 worldPos = vec3(vec4(pos, 1) * m) + (camRight * position.x * scale.x) + (camUp * position.y);
		
		gl_Position = vec4(worldPos, 1.0) * vp;
	}
	else {
		gl_Position = (vec4(position.x,position.y,0,1.0) * m + vec4(pos,1.0) - m[3]) * vp;
	}
}