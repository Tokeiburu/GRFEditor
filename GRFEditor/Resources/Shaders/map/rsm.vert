#version 330 core

layout (location = 0) in vec3 a_position;
layout (location = 1) in vec2 a_texture;
layout (location = 2) in vec3 a_normal;
layout (location = 3) in float a_cull;
layout (location = 4) in mat4 a_instanceMatrix;

//uniform mat4 u_MeshMatrices[64];

uniform mat4 vp;
uniform mat4 m;
uniform mat4 instanceMatrix;

uniform int shadeType;
uniform vec3 lightDiffuse;
uniform vec3 lightAmbient;
uniform vec3 lightDirection;
uniform bool useInstances;

out vec2 texCoord;
out vec3 normal;
out vec3 FragPos;
out vec3 mult;
out float cull;

void main(void)
{
	mat4 instanceMat = useInstances ? a_instanceMatrix : instanceMatrix;
	//mat4 instanceMat = a_instanceMatrix;
	
	mat3 normalMatrix = mat3(m * instanceMat);
	normal = a_normal * normalMatrix;
	normal = normalize(normal);
	cull = a_cull;
	
	if (useInstances && cull < 1) {
		float determinant = determinant(mat3(instanceMat));
		cull = determinant < 0.0 ? -1.0 : 0.0;
	}
	
	texCoord = a_texture;
	gl_Position = vec4(a_position, 1.0) * m * instanceMat * vp;
	FragPos = vec3(vec4(a_position, 1.0) * m * instanceMat);
	
	if (shadeType != 0 && shadeType != 4) {
		float NL = clamp(dot(normalize(normal), lightDirection),0.0,1.0);
		vec3 ambientFactor = (1.0 - lightAmbient) * lightAmbient;
		vec3 ambient = lightAmbient - ambientFactor + ambientFactor * lightDiffuse;
		vec3 diffuseFactor = (1.0 - lightDiffuse) * lightDiffuse;
		vec3 diffuse = lightDiffuse - diffuseFactor + diffuseFactor * lightAmbient;
		vec3 mult1 = min(NL * diffuse + ambient, 1.0);
		vec3 mult2 = min(max(lightDiffuse, lightAmbient) + (1.0 - max(lightDiffuse, lightAmbient)) * min(lightDiffuse, lightAmbient), 1.0);
		mult = min(mult1, mult2);
	}
}