#version 330

out vec4 fragColor;
in vec2 texCoord;

uniform float alpha;
uniform sampler2D s_texture;

void main()
{
    vec4 c = texture2D(s_texture, texCoord);
	c = vec4(c.rgb * alpha, alpha);
	fragColor = c;
}