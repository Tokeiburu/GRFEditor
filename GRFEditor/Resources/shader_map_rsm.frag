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

in vec2 texCoord;
in vec3 normal;
in vec3 FragPos;

void main()
{
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
		color.rgb *= min(lightDiffuse, 1.0 - lightAmbient) * lightDiffuse + lightAmbient;
	}
	else {
		float NL = clamp(dot(normalize(normal), lightDirection),0.0,1.0);
		color.rgb *= NL * min(lightDiffuse, 1.0 - lightAmbient) * lightDiffuse + lightAmbient;
	}
	
	gl_FragData[0] = color;
}