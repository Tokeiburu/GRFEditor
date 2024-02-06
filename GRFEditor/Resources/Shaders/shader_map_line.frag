#version 330

out vec4 outputColor;

uniform vec4 colorMult;

void main()
{
    outputColor = colorMult;
}