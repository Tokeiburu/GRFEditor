#version 420

layout (location = 0) in vec3 aQuadPos;
layout (location = 1) in vec2 aQuadUv;
layout (location = 2) in float aSeed;

uniform mat4 vp;

layout(std140, binding = 0) uniform ParticleParams
{
	uniform float a_particleNum;
	uniform float a_radius;
	uniform float a_thickness;
	uniform float a_height;
	uniform float a_speed;
	uniform float a_fullAngle;
	uniform vec2 a_rotateVector;
	uniform vec3 a_basePosition;
	uniform float pad1;
};

uniform float uTime;
uniform vec4 color;

out vec2 texCoord;
out float alpha;

float hash(float n, float g) {
	return fract(sin(n * 100.0 * (g + 1) + g) * 43758.5453123);
}

void main(void)
{
	texCoord = aQuadUv;
	
	float duration = 8.0 * color.a;
	float halfDuration = duration * 0.5;
	float seed = hash(aSeed, 1);
	
	if (duration <= 0.0) {
		alpha = 0.0;
	}
	else {
		float time = mod(uTime + duration * seed, duration);
		
		if (time < halfDuration)
			alpha = time / halfDuration;
		else
			alpha = (duration - time) / halfDuration;
	}
	
	seed = hash(aSeed, 2);
	
	// A full revolution takes roughly 6 seconds at x1 speed
	float r = mod(seed + uTime / 6.0 * a_speed, 1.0);
	
	float angle = aQuadPos.x * radians(a_fullAngle) + r * 2.0 * 3.14159265;
	float radius = a_radius;
	
	if (aQuadPos.y < 0.0) {
		radius += a_thickness;
	}
	
	vec3 pos;
	pos.x = cos(angle) * radius;
	pos.z = sin(angle) * radius;
	pos.y = aQuadPos.y * a_thickness;
	
	if (a_particleNum > 1) {
		int id = gl_InstanceID;
		
		pos.y += id / (a_particleNum - 1.0) * a_height;
	}
	
	float rx = radians(a_rotateVector.x);
	float rz = -radians(a_rotateVector.y);
	
	// Rotate around X
	float y = pos.y * cos(rx) - pos.z * sin(rx);
	float z = pos.y * sin(rx) + pos.z * cos(rx);
	pos.y = y;
	pos.z = z;
	
	// Rotate around Z
	float x = pos.x * cos(rz) - pos.y * sin(rz);
	y = pos.x * sin(rz) + pos.y * cos(rz);
	pos.x = x;
	pos.y = y;
	
	vec3 worldPos = a_basePosition + pos;

	gl_Position = vec4(worldPos, 1.0) * vp;
}