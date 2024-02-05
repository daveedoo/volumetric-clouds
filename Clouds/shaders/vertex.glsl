#version 330 core
layout(location = 0) in vec2 pos;
out vec3 rayDir;

uniform mat4 projMtx;
uniform mat4 viewMtx;

void main()
{
	gl_Position = vec4(pos, 0.5, 1.0);

	float projXInv = 1 / projMtx[0][0];
    float projYInv = 1 / projMtx[1][1];
    
    // Conevert point pos from persepctive to camera and world
    vec4 viewVec = vec4(projXInv * pos.x, projYInv * pos.y, -1, 0);
    viewVec = inverse(viewMtx) * viewVec;
	rayDir = normalize(viewVec.xyz);
}
