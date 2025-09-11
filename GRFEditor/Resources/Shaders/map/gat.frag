#version 330

out vec4 fragColor;
in vec2 texCoord;

uniform float alpha;
uniform vec4 colorMult = vec4(1,1,1,1);
uniform sampler2D s_texture;
uniform float textureFac = 0.0f;

in vec3 normal;
uniform vec3 lightDirection = vec3(1, 1, 1);
uniform float lightMin = 0.5;

void main()
{
	//if (!gl_FrontFacing)
	//	discard;
	
	//vec4 c = mix(color, texture2D(s_texture, texCoord), textureFac) * colorMult;
    vec4 c = texture2D(s_texture, texCoord);
	c = vec4(c.rgb * alpha, alpha);
	fragColor = c;
}