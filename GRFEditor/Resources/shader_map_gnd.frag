#version 330

uniform sampler2D s_texture;
uniform sampler2D s_lighting;

uniform vec3 lightDiffuse;
uniform vec3 lightAmbient;
uniform vec3 lightDirection;
uniform float lightIntensity;
uniform float showShadowmap = 1.0;
uniform float showLightmap = 1.0;

in vec2 texCoord;
in vec2 texCoord2;
in vec3 normal;
in vec4 color;

out vec4 fragColor;

void main()
{
	vec4 texture = vec4(1,1,1,1);
	vec4 texColor = texture2D(s_texture, texCoord);
	texture = texColor;
	
	if(texture.a < 0.1)
		discard;
	
	// Rsw directional light|diffuse+ambient
	float NL = clamp(dot(normalize(normal), vec3(1,-1,1)*lightDirection),0.0,1.0);
	vec3 mv = vec3(min(lightDiffuse.r, 1.0 - lightAmbient.r), min(lightDiffuse.g, 1.0 - lightAmbient.g), min(lightDiffuse.b, 1.0 - lightAmbient.b));
	texture.rgb *= (NL * mv * lightDiffuse + lightAmbient);
	
	// Tile color is renderer before lightmap
	texture.rgb *= max(color.rgb, 1.0 - showLightmap);
	// Shadowmap
	texture.rgb *= max(texture2D(s_lighting, texCoord2).a, 1.0 - showShadowmap);
	// Lightmap - Drawn on top of the Shadowmap
	texture.rgb += clamp(texture2D(s_lighting, texCoord2).rgb, 0.0, 1.0) * showLightmap;
	
	fragColor = texture;
}