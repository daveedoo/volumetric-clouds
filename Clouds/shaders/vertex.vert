#version 330 core
layout(location = 0) in vec2 pos;
out vec3 viewVec;

uniform mat4 projMtx;
uniform mat4 viewMtx;

void main()
{
	gl_Position = vec4(pos, 0.5, 1.0);

	vec4 view = inverse(viewMtx) * inverse(projMtx) * vec4(pos, 1, 0);
	viewVec = normalize(view.xyz);
}
