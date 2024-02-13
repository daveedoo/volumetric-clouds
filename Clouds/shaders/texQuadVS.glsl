#version 330 core
layout(location = 0) in vec2 pos;
out vec2 texCoord;

void main()
{
	gl_Position = vec4(pos.xy, 0.0f, 1.0f);
	texCoord = vec2(pos.x / 2.f + 0.5f, pos.y / 2.f + 0.5f);
}
