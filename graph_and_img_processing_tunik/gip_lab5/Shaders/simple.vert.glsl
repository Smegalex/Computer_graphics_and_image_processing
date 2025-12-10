#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;
layout(location = 2) in vec2 aTexCoord;
layout(location = 3) in float aUseTexture;

out vec3 vColor;
out vec2 vTexCoord;
out float vUseTexture;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform float uTexScale; // texture coordinate scale multiplier
uniform float uTexOffset; // texture coordinate horizontal offset for animation

void main()
{
    vColor = aColor;
    vTexCoord = aTexCoord * uTexScale + vec2(uTexOffset, 0.0);
    vUseTexture = aUseTexture;
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
}
