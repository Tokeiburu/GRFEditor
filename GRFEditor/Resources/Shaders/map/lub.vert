#version 330 core

layout (location = 0) in vec2 a_position;
layout (location = 1) in float a_alpha;
layout (location = 2) in vec3 pos;

uniform mat4 projectionMatrix;
uniform mat4 cameraMatrix;
uniform mat4 m;
uniform float billboard_off;

out vec2 texCoord;
out float alpha;

void main(void)
{
	texCoord = vec2(a_position.x < 0 ? 0 : 1, a_position.y < 0 ? 0 : 1);
	alpha = a_alpha;
	
	if (billboard_off == 0) {
		vec4 posf = vec4(m[0].x, m[1].x, m[2].x, m[3].x) * pos.x + vec4(m[0].y, m[1].y, m[2].y, m[3].y) * pos.y + vec4(m[0].z, m[1].z, m[2].z, m[3].z) * pos.z + vec4(m[0].w, m[1].w, m[2].w, m[3].w);
		
		mat4 m2 = mat4(
			m[0].x, m[0].y, m[0].z, posf.x,
			m[1].x, m[1].y, m[1].z, posf.y,
			m[2].x, m[2].y, m[2].z, posf.z,
			m[3].x, m[3].y, m[3].z, posf.w
		);
		
		vec4 billboarded = vec4(0.0,0.0,0.0,1.0) * m2 * cameraMatrix * projectionMatrix;
		billboarded.xy += (vec4(a_position.x, a_position.y,0.0,1.0) * projectionMatrix).xy;
		gl_Position = billboarded;
	}
	else {
		vec4 billboarded = (vec4(a_position.x,a_position.y,0,1.0) * m + vec4(pos,1.0) - m[3]) * cameraMatrix * projectionMatrix;
		gl_Position = billboarded;
	}
}