#version 330

out vec4 fragColor;
in vec2 texCoord;

uniform sampler2D s_texture;

void main()
{
    vec4 color = texture2D(s_texture, texCoord); 
    color.a = 0.564;
    fragColor = color;
}