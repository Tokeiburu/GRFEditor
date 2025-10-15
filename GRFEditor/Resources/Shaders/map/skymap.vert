#version 420

layout (location = 0) in vec3 aQuadPos;
layout (location = 1) in vec2 aQuadUv;
layout (location = 2) in vec3 aPosition;
layout (location = 3) in float aSeed;
layout (location = 4) in float aLifeStart;
layout (location = 5) in float aLifeDuration;
layout (location = 6) in float aLifeEnd;
layout (location = 7) in float aExpandDelay;
layout (location = 8) in float aUVStart;

uniform mat4 view;
uniform mat4 vp;

layout(std140, binding = 0) uniform ParticleParams
{
	uniform float aSize;
	uniform float aSize_Extra;
	uniform float aHeight;
	uniform float aHeight_Extra;
	uniform float aAlpha_Inc_Time;
	uniform float aAlpha_Inc_Time_Extra;
	uniform float aAlpha_Inc_Speed;
	uniform float aAlpha_Dec_Time;
	uniform float aAlpha_Dec_Time_Extra;
	uniform float aAlpha_Dec_Speed;
	uniform float aExpand_Rate;
	uniform float aUVSplit;
	uniform float aUVCycleSpeed;
	uniform float aScaleX;
	uniform float aScaleY;
	uniform int aDirMode;
	uniform vec3 aForcedDir;
};

uniform float aGndWidth;
uniform float aGndHeight;

uniform float uTime;

out vec2 texCoord;
out float alpha;

float hash(float n, float g) {
	return fract(sin(n * 100.0 * (g + 1) + g) * 43758.5453123);
}

vec3 randomDirection(float seed, float index) {
	float angle = hash(seed, index) * 6.2831853; // [0, 2p)
	return normalize(vec3(cos(angle), 0.0, sin(angle)));
}

float easeInOutPow(float t, float p) {
    if (t < 0.5)
        return 0.5 * pow(2.0 * t, p);
	else
        return 0.5 * (2.0 - pow(2.0 * (1.0 - t), p));
}

void main(void)
{
	float t = uTime - aLifeStart;
	
	texCoord = aQuadUv;
	
	if (aUVCycleSpeed <= 0 || aUVSplit <= 1.0f) {
		if (texCoord.y <= 0.0)
			texCoord.y = aUVStart + (1.0 / aUVSplit);
		else if (texCoord.y >= 1.0)
			texCoord.y = aUVStart;
	}
	else {
		float texIdx = aUVStart + floor(t / aUVCycleSpeed);
		float uvStart = mod(texIdx, aUVSplit) / aUVSplit;
		if (texCoord.y <= 0.0)
			texCoord.y = uvStart + (1.0 / aUVSplit);
		else if (texCoord.y >= 1.0)
			texCoord.y = uvStart;
	}
	
	float size = aSize + aSize_Extra * hash(aSeed, 6);
	
	if (aExpand_Rate > 0.0) {
		// The expand value stays after iteration, so it's bound to... well, something that has to be added, I guess, from [0..6]
		float expandDur = 6.0 / 1.6;
		float expandDurHalf = expandDur / 2.0;
		float eTime = mod(uTime + aExpandDelay, expandDur);
		
		float minSizeMult = 1 - aExpand_Rate;
		float maxSizeMult = 1 + aExpand_Rate;
		
		if (eTime < expandDurHalf) {
			size *= minSizeMult + smoothstep(0.0, 1.0, eTime / expandDurHalf) * aExpand_Rate * 2.0;
		}
		else {
			size *= maxSizeMult - smoothstep(0.0, 1.0, (eTime - expandDurHalf) / expandDurHalf) * aExpand_Rate * 2.0;
		}
	}
	
	vec3 position = aPosition;
	
	if (aHeight < 99999.0)
		position.y = -aHeight;
	
	if (aDirMode == 0) {
		float speed = 8;
		float blendTime = 1.0;
		float interval = 3.0;
		float ft = fract(t / interval);
		float step = floor(t / interval);
		
		float smoothT = easeInOutPow(ft, 1.3);
		vec3 dirPrev = normalize(randomDirection(aSeed, step));
		vec3 dirNext = normalize(randomDirection(aSeed, step + 1));
		
		vec3 offset = mix(dirPrev, dirNext, smoothT) * speed;
		
		position += offset;
	}
	else if (aDirMode == 1) {
		// No movement
	}
	else if (aDirMode == 2) {
		position += t * aForcedDir.xyz * 1.6;
	}
	
	float Alpha_Inc_Time = (aAlpha_Inc_Time + aSeed * aAlpha_Inc_Time_Extra) / 100.0;
	
	if (t < Alpha_Inc_Time) {
		alpha = min(2.55, t * aAlpha_Inc_Speed);
	}
	else {
		alpha = min(2.55, Alpha_Inc_Time * aAlpha_Inc_Speed);
		
		float dec_time = max(Alpha_Inc_Time, aLifeEnd);
		
		if (t >= dec_time) {
			alpha = max(0, alpha - (t - dec_time) * aAlpha_Dec_Speed);
			
			//if (aAlpha_Dec_Speed < 0 && alpha > 255)
			//	alpha = mod(alpha, 255);
		}
	}
	
	alpha = alpha / 2.55;
	
	position.y -= aHeight_Extra * hash(aSeed, 2);
	
	vec3 camRight = vec3(view[0][0], view[0][1], view[0][2]);
    vec3 camUp    = vec3(view[1][0], view[1][1], view[1][2]);
	vec3 worldPos = position + (camRight * aQuadPos.x * size * aScaleX) + (camUp * aQuadPos.y * size * aScaleY);
	
	gl_Position = vec4(worldPos, 1.0) * vp;
}