#version 330

out vec4 outputColor;
in vec2 texCoord;

uniform vec4 colorMult3;
uniform sampler2D texture0;

void main()
{
	outputColor = texture(texture0, texCoord);
    
	outputColor.rgb = (1 - colorMult3.a) * outputColor.rgb + colorMult3.a * colorMult3.rgb;
	outputColor.a = 1;
}