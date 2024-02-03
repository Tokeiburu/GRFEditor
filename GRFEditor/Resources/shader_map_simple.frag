#version 330

out vec4 fragColor;

uniform vec4 color;
uniform vec4 colorMult = vec4(1,1,1,1);

void main()
{
    vec4 c = color * colorMult;
	fragColor = c;
}