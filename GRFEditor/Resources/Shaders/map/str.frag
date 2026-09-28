#version 420

uniform sampler2D s_texture;
uniform vec4 color = vec4(1,1,1,1);
in vec2 texCoord;
out vec4 fragColor;
uniform float alpha;
uniform bool selection;
uniform vec4 selectionColor = vec4(1,1,0.5,0.8f);

void main()
{
	if (selection) {
		fragColor = selectionColor;
	}
	else {
		vec4 outColor = texture2D(s_texture, texCoord);
		
		if (outColor.r == 0 && outColor.g == 0 && outColor.b == 0) {
			discard;
		}
		
		outColor *= color;
		outColor.a *= alpha;
		fragColor = outColor;
	}
	
}