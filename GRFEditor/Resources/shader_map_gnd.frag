#version 330

uniform sampler2D s_texture;
uniform sampler2D s_lighting;

uniform vec3 lightDiffuse;
uniform vec3 lightAmbient;
uniform vec3 lightDirection;
uniform float lightIntensity;
uniform float showShadowmap = 1.0;
uniform float showLightmap = 1.0;
uniform float enableCullFace = 0.0;

in vec2 texCoord;
in vec2 texCoord2;
in vec3 normal;
in vec4 color;

out vec4 fragColor;

void main()
{
	if (enableCullFace > 0 && !gl_FrontFacing) {
		discard;
	}
	
	vec4 texture = vec4(1,1,1,1);
	vec4 texColor = texture2D(s_texture, texCoord);
	texture = texColor;
	
	if(texture.a < 0.1)
		discard;
	
	// Rsw directional light|diffuse+ambient
	float NL = clamp(dot(normalize(normal), vec3(1,-1,1)*lightDirection),0.0,1.0);
	vec3 ambientFactor = (1.0 - lightAmbient) * lightAmbient;
	vec3 ambient = lightAmbient - ambientFactor + ambientFactor * lightDiffuse;
	vec3 diffuseFactor = (1.0 - lightDiffuse) * lightDiffuse;
	vec3 diffuse = lightDiffuse - diffuseFactor + diffuseFactor * lightAmbient;
	vec3 mult1 = min(NL * diffuse + ambient, 1.0);
	// The formula quite literally changes when the combined value of lightAmbient + lightDiffuse is greater than 1.0
	vec3 mult2 = min(max(lightDiffuse, lightAmbient) + (1.0 - max(lightDiffuse, lightAmbient)) * min(lightDiffuse, lightAmbient), 1.0);
	texture.rgb *= min(mult1, mult2);
	
	// Tile color is renderer before lightmap
	texture.rgb *= max(color.rgb, 1.0 - showLightmap);
	// Shadowmap
	texture.rgb *= max(texture2D(s_lighting, texCoord2).a, 1.0 - showShadowmap);
	// Lightmap - Drawn on top of the Shadowmap
	texture.rgb += clamp(texture2D(s_lighting, texCoord2).rgb, 0.0, 1.0) * showLightmap;
	
	fragColor = texture;
}