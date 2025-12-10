#version 330 core

in vec3 vColor;
in vec2 vTexCoord;
in float vUseTexture;
out vec4 FragColor;

uniform sampler2D uTexture;

void main()
{
    if (vUseTexture > 0.5)
    {
        FragColor = texture(uTexture, vTexCoord);
    }
    else
    {
        FragColor = vec4(vColor, 1.0);
    }
}
