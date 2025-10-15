#version 330

uniform sampler2D s_texture;
uniform vec4 color = vec4(1,1,1,1);
in vec2 texCoord;
in float alpha;
out vec4 fragColor;

void main()
{
	vec4 outColor = texture2D(s_texture, texCoord);
	
	if (outColor.a <= 0)
		discard;
	
	outColor *= color;
	outColor.a = alpha;
	fragColor = outColor;
}