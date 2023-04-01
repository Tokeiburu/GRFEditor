#version 330

out vec4 outputColor;
in vec2 texCoord;

uniform vec4 colorMult3;
uniform sampler2D texture0;

void main()
{
    // To use a texture, you call the texture() function.
    // It takes two parameters: the sampler to use, and a vec2, used as texture coordinates.
	outputColor = texture(texture0, texCoord);
    
    outputColor.r = (1 - colorMult3.a) * outputColor.r + colorMult3.a * colorMult3.r;
    outputColor.g = (1 - colorMult3.a) * outputColor.g + colorMult3.a * colorMult3.g;
    outputColor.b = (1 - colorMult3.a) * outputColor.b + colorMult3.a * colorMult3.b;
	outputColor.a = 1;
}