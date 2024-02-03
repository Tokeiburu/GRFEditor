#version 330

uniform sampler2D s_texture;

uniform vec3 lightDiffuse;
uniform vec3 lightAmbient;
uniform vec3 lightDirection;
uniform vec3 lightPosition;
uniform float lightIntensity;

uniform int shadeType;
uniform float discardValue = 0.8;

uniform vec2 texTranslate = vec2(0);
uniform vec2 texMult = vec2(1);
uniform mat4 texRot;
uniform float textureAnimToggle;
uniform float billboard_off;
uniform float enableCullFace = 0.0;

in vec2 texCoord;
in vec3 normal;
in vec3 FragPos;
in float cull;

void main()
{
	if (enableCullFace > 0 && cull <= 0) {
		if (cull >= 0 && !gl_FrontFacing)
			discard;
		if (cull < 0 && gl_FrontFacing)
			discard;
	}

	vec2 texCoord2 = texCoord;
	
	if (textureAnimToggle == 1) {
		texCoord2 = (texCoord + texTranslate) * texMult;
		texCoord2 = texCoord2 - vec2(0.5f, 0.5f);
		texCoord2 = vec2(texRot * vec4(texCoord2.x, texCoord2.y, 0, 1));
		texCoord2 = texCoord2 + vec2(0.5f, 0.5f);
	}
	
	vec4 color = texture2D(s_texture, texCoord2);
	
	if(color.a < discardValue)
		discard;
		
	if (shadeType == 4) {	// for editor
		vec3 lightDir = normalize(lightPosition - FragPos);
		vec3 normal2 = normal;
		
		if (!gl_FrontFacing) {
			normal2 = normal2 * -1;
		}
		
		vec3 diffuse = max(dot(normal2, lightDir), 0.0) * vec3(1);
		color.rgb = color.rgb * lightAmbient + (1 - lightAmbient) * diffuse * color.rgb;
	}
	else if (shadeType == 0) {
		color.rgb *= min(max(lightDiffuse, lightAmbient) + (1.0 - max(lightDiffuse, lightAmbient)) * min(lightDiffuse, lightAmbient), 1.0);
	}
	else {
		float NL = clamp(dot(normalize(normal), lightDirection),0.0,1.0);
		vec3 ambientFactor = (1.0 - lightAmbient) * lightAmbient;
		vec3 ambient = lightAmbient - ambientFactor + ambientFactor * lightDiffuse;
		vec3 diffuseFactor = (1.0 - lightDiffuse) * lightDiffuse;
		vec3 diffuse = lightDiffuse - diffuseFactor + diffuseFactor * lightAmbient;
		vec3 mult1 = min(NL * diffuse + ambient, 1.0);
		vec3 mult2 = min(max(lightDiffuse, lightAmbient) + (1.0 - max(lightDiffuse, lightAmbient)) * min(lightDiffuse, lightAmbient), 1.0);
		color.rgb *= min(mult1, mult2);
	}
	
	gl_FragData[0] = color;
}