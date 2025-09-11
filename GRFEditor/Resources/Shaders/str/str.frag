#version 330

out vec4 outputColor;

in vec2 texCoord;
uniform vec4 colorMult;
uniform sampler2D texture0;

void main()
{
    outputColor = texture(texture0, texCoord);
	
	if (outputColor.r < 0.1 && outputColor.g < 0.1 && outputColor.b < 0.1) {
		discard;
	}
	
	outputColor = outputColor * colorMult;
}