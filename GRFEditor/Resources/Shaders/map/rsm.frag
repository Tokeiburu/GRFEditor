#version 330

uniform sampler2D s_texture;

uniform vec3 lightDiffuse;
uniform vec3 lightAmbient;
uniform vec3 lightDirection;
uniform vec3 lightPosition;
uniform vec4 wireframeColor;

uniform int shadeType;
uniform float discardValue = 0.8;
uniform int discardAlphaMode = 0;

uniform mat4 texRot;
uniform float textureAnimToggle;
uniform float billboard_off;
uniform float enableCullFace = 0.0;
uniform float opacity = 1.0;
uniform bool wireframe = false;

in vec2 texCoord;
in vec3 normal;
in vec3 FragPos;
in vec3 mult;
in float cull;
out vec4 fragColor;

void main()
{
	if (!wireframe && enableCullFace > 0 && cull <= 0) {
		if (cull >= 0 && !gl_FrontFacing)
			discard;
		if (cull < 0 && gl_FrontFacing)
			discard;
	}

	vec2 texCoord2 = texCoord;
	
	if (textureAnimToggle == 1) {
		texCoord2 = vec2(vec4(texCoord2.x, texCoord2.y, 0, 1) * texRot);
	}
	
	vec4 color = texture2D(s_texture, texCoord2);
	
	if (discardAlphaMode == 1 && color.a < 1)
		discard;
	if (discardAlphaMode == 2 && color.a >= 1)
		discard;
	if (color.a < discardValue)
		discard;
		
	color.a *= opacity;
		
	if (wireframe) {
		color = wireframeColor;
		
		if (shadeType == 0) {
			
		}
		else if (color.r > 0.0) {
			vec3 lightDir = normalize(lightPosition - FragPos);
			vec3 ambient = vec3(0.5, 0.5, 0.5);
			vec3 diffuse = abs(dot(normal, lightDir)) * vec3(1);
			color.rgb = color.rgb * ambient + (1 - ambient) * diffuse * color.rgb;
		}
	}
	else if (shadeType == 4) {	// for editor
		vec3 lightDir = normalize(lightPosition - FragPos);
		vec3 normal2 = normal;
		
		if (!gl_FrontFacing) {
			normal2 = normal2 * -1;
		}
		
		vec3 diffuse = abs(dot(normal, lightDir)) * vec3(1);
		color.rgb = color.rgb * lightAmbient + (1 - lightAmbient) * diffuse * color.rgb;
	}
	else if (shadeType == 0) {
		color.rgb *= min(max(lightDiffuse, lightAmbient) + (1.0 - max(lightDiffuse, lightAmbient)) * min(lightDiffuse, lightAmbient), 1.0);
	}
	else {
		color.rgb *= mult;
	}
	
	fragColor = color;
}