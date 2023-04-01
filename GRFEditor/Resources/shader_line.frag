#version 330

out vec4 outputColor;

uniform vec4 colorMult2;

void main()
{
    // To use a texture, you call the texture() function.
    // It takes two parameters: the sampler to use, and a vec2, used as texture coordinates.
    outputColor = colorMult2;
}