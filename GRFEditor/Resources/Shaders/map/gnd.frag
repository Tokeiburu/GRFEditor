#version 330

uniform sampler2D s_texture;
uniform sampler2D s_lighting;

uniform float lightIntensity;
uniform float showShadowmap = 1.0;
uniform float showLightmap = 1.0;
uniform float enableCullFace = 0.0;
uniform bool fixedColor = false;

in vec2 texCoord;
in vec2 texCoord2;
in vec3 normal;
in vec4 color;
in vec3 mult;

out vec4 fragColor;

void main()
{
	if (!fixedColor && enableCullFace > 0 && !gl_FrontFacing) {
		discard;
	}
	
	vec4 texture = vec4(1,1,1,1);
	vec4 texColor = texture2D(s_texture, texCoord);
	texture = texColor;
	
	if(texture.a < 0.1)
		discard;
	
	texture.rgb *= mult;
	
	// Tile color is renderer before lightmap
	texture.rgb *= max(color.rgb, 1.0 - showLightmap);
	// Shadowmap
	texture.rgb *= max(texture2D(s_lighting, texCoord2).a, 1.0 - showShadowmap);
	// Lightmap - Drawn on top of the Shadowmap
	texture.rgb += clamp(texture2D(s_lighting, texCoord2).rgb, 0.0, 1.0) * showLightmap;
	
	fragColor = texture;
	
	if (fixedColor)
		fragColor = vec4(0, 0, 0, 1);
}