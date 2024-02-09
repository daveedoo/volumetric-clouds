#version 460 core

layout (local_size_x = 1, local_size_y =1, local_size_z=1) in;


layout(rgba32f, binding=1) uniform image3D shape;
layout(rgba32f, binding=2) uniform image3D detail;

uniform vec4 shapeSettings;
uniform vec4 detailSettings;
uniform int texSize;

// code from shadertoy: https://www.shadertoy.com/view/wsX3D7


float modValue = 512.0;

float permuteX(float x)
{
	float t = ((x*67.0)+71.0)*x;
	return mod(t,modValue);
}

float permuteY(float x)
{
    float t = ((x * 73.0) + 83.0) * x;
	return mod(t, modValue);
}

float permuteZ(float x)
{
    float t = ((x * 103.0) + 109.0) * x;
	return mod(t, modValue);
}

float shiftX(float value)
{
    return fract(value * (1.0 / 73.0)) * 2.0 - 1.0;
}

float shiftY(float value)
{
    return fract(value * (1.0 / 69.0)) * 2.0 - 1.0;
}

float shiftZ(float value)
{
    return fract(value * (1.0 / 89.0)) * 2.0 - 1.0;
}

float taylorInvSqrt(float r)
{
	return 1.79284291400159 - 0.85373472095314 * r;
}

float smoothmix(float x, float y, float t)
{
	t = t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
    return y * t + x * (1.0f - t);
}

float perlinNoise(vec3 c)
{
    vec3 ci = floor(c.xyz);
    vec3 cr = fract(c.xyz);
    
    vec3 i000 = ci;
    vec3 i001 = ci + vec3(0.0f, 0.0f, 1.0f);
    vec3 i010 = ci + vec3(0.0f, 1.0f, 0.0f);
    vec3 i011 = ci + vec3(0.0f, 1.0f, 1.0f);
    vec3 i100 = ci + vec3(1.0f, 0.0f, 0.0f);
    vec3 i101 = ci + vec3(1.0f, 0.0f, 1.0f);
    vec3 i110 = ci + vec3(1.0f, 1.0f, 0.0f);
    vec3 i111 = ci + vec3(1.0f, 1.0f, 1.0f);
    
    i000 = mod(i000, modValue);
    i001 = mod(i001, modValue);
    i010 = mod(i010, modValue);
    i011 = mod(i011, modValue);
    i100 = mod(i100, modValue);
    i101 = mod(i101, modValue);
    i110 = mod(i110, modValue);
    i111 = mod(i111, modValue);
    
    float rX000 = permuteX(permuteX(permuteX(i000.x) + i000.y) + i000.z);
    float rX001 = permuteX(permuteX(permuteX(i001.x) + i001.y) + i001.z);
    float rX010 = permuteX(permuteX(permuteX(i010.x) + i010.y) + i010.z);
    float rX011 = permuteX(permuteX(permuteX(i011.x) + i011.y) + i011.z);
    float rX100 = permuteX(permuteX(permuteX(i100.x) + i100.y) + i100.z);
    float rX101 = permuteX(permuteX(permuteX(i101.x) + i101.y) + i101.z);
    float rX110 = permuteX(permuteX(permuteX(i110.x) + i110.y) + i110.z);
    float rX111 = permuteX(permuteX(permuteX(i111.x) + i111.y) + i111.z);
    
    float rY000 = permuteY(permuteY(permuteY(i000.x) + i000.y) + i000.z);
    float rY001 = permuteY(permuteY(permuteY(i001.x) + i001.y) + i001.z);
    float rY010 = permuteY(permuteY(permuteY(i010.x) + i010.y) + i010.z);
    float rY011 = permuteY(permuteY(permuteY(i011.x) + i011.y) + i011.z);
    float rY100 = permuteY(permuteY(permuteY(i100.x) + i100.y) + i100.z);
    float rY101 = permuteY(permuteY(permuteY(i101.x) + i101.y) + i101.z);
    float rY110 = permuteY(permuteY(permuteY(i110.x) + i110.y) + i110.z);
    float rY111 = permuteY(permuteY(permuteY(i111.x) + i111.y) + i111.z);
    
    float rZ000 = permuteZ(permuteZ(permuteZ(i000.x) + i000.y) + i000.z);
    float rZ001 = permuteZ(permuteZ(permuteZ(i001.x) + i001.y) + i001.z);
    float rZ010 = permuteZ(permuteZ(permuteZ(i010.x) + i010.y) + i010.z);
    float rZ011 = permuteZ(permuteZ(permuteZ(i011.x) + i011.y) + i011.z);
    float rZ100 = permuteZ(permuteZ(permuteZ(i100.x) + i100.y) + i100.z);
    float rZ101 = permuteZ(permuteZ(permuteZ(i101.x) + i101.y) + i101.z);
    float rZ110 = permuteZ(permuteZ(permuteZ(i110.x) + i110.y) + i110.z);
    float rZ111 = permuteZ(permuteZ(permuteZ(i111.x) + i111.y) + i111.z);
    
    float x000 = shiftX(rX000);
    float x001 = shiftX(rX001);
    float x010 = shiftX(rX010);
    float x011 = shiftX(rX011);
    float x100 = shiftX(rX100);
    float x101 = shiftX(rX101);
    float x110 = shiftX(rX110);
    float x111 = shiftX(rX111);
    
    float y000 = shiftY(rY000);
    float y001 = shiftY(rY001);
    float y010 = shiftY(rY010);
    float y011 = shiftY(rY011);
    float y100 = shiftY(rY100);
    float y101 = shiftY(rY101);
    float y110 = shiftY(rY110);
    float y111 = shiftY(rY111);
    
    float z000 = shiftZ(rZ000);
    float z001 = shiftZ(rZ001);
    float z010 = shiftZ(rZ010);
    float z011 = shiftZ(rZ011);
    float z100 = shiftZ(rZ100);
    float z101 = shiftZ(rZ101);
    float z110 = shiftZ(rZ110);
    float z111 = shiftZ(rZ111);
    
	vec3 g000 = vec3(x000, y000, z000);
	vec3 g001 = vec3(x001, y001, z001);
	vec3 g010 = vec3(x010, y010, z010);
	vec3 g011 = vec3(x011, y011, z011);
	vec3 g100 = vec3(x100, y100, z100);
	vec3 g101 = vec3(x101, y101, z101);
	vec3 g110 = vec3(x110, y110, z110);
	vec3 g111 = vec3(x111, y111, z111);
     
    float n000 = taylorInvSqrt(dot(g000, g000));
    float n001 = taylorInvSqrt(dot(g001, g001));
    float n010 = taylorInvSqrt(dot(g010, g010));
    float n011 = taylorInvSqrt(dot(g011, g011));
    float n100 = taylorInvSqrt(dot(g100, g100));
    float n101 = taylorInvSqrt(dot(g101, g101));
    float n110 = taylorInvSqrt(dot(g110, g110));
    float n111 = taylorInvSqrt(dot(g111, g111));
    
    g000 *= n000;
    g001 *= n001;
    g010 *= n010;
    g011 *= n011;
    g100 *= n100;
    g101 *= n101;
    g110 *= n110;
    g111 *= n111;
    
    float f000 = dot(g000, cr);
    float f001 = dot(g001, cr - vec3(0.0f, 0.0f, 1.0f));
    float f010 = dot(g010, cr - vec3(0.0f, 1.0f, 0.0f));
    float f011 = dot(g011, cr - vec3(0.0f, 1.0f, 1.0f));
    float f100 = dot(g100, cr - vec3(1.0f, 0.0f, 0.0f));
    float f101 = dot(g101, cr - vec3(1.0f, 0.0f, 1.0f));
    float f110 = dot(g110, cr - vec3(1.0f, 1.0f, 0.0f));
    float f111 = dot(g111, cr - vec3(1.0f, 1.0f, 1.0f));
    
    float fadeX0 = smoothmix(f000, f100, cr.x);
    float fadeX1 = smoothmix(f010, f110, cr.x);
    float fadeX2 = smoothmix(f001, f101, cr.x);
    float fadeX3 = smoothmix(f011, f111, cr.x);
    float fadeY0 = smoothmix(fadeX0, fadeX1, cr.y);
    float fadeY1 = smoothmix(fadeX2, fadeX3, cr.y);
    float fadeZ0 = smoothmix(fadeY0, fadeY1, cr.z);
    
    return fadeZ0 * 2.3;
}


    // voronoi noise generator for tests

    float hash(float x)
    {
        return fract(x+1.3215 * 1.8152);
    }

    float hash3(vec3 a)
    {
        return fract((hash(a.z * 42.8883) + hash(a.y * 36.9125) + hash(a.x * 65.4321)) * 291.1257);
    }

    vec3 rehash3(float x)
    {
        return vec3(hash(((x + 0.5283) * 59.3829) * 274.3487), hash(((x + 0.8192) * 83.6621) * 345.3871), hash(((x + 0.2157f) * 36.6521f) * 458.3971f));
    }

    float sqr(float x)
    { 
        return x * x;
    }

    float fastdist(vec3 a, vec3 b)
    {
        return sqr(b.x - a.x) + sqr(b.y - a.y) + sqr(b.z - a.z);
    }

    vec2 eval(float x, float y, float z)
    {
        vec4 p[27];
        for (int _x = -1; _x <= 1; _x++)
            for (int _y = -1; _y <= 1; _y++)
                for (int _z = -1; _z <= 1; _z++)
                {
                    vec3 _p = vec3(floor(x), floor(y), floor(z)) + vec3(_x, _y, _z);
                    float h = hash3(_p);
                    p[(_x + 1) + ((_y + 1) * 3) + ((_z + 1) * 3 * 3)] = vec4((rehash3(h) + _p).xyz, h);
                }
        float m = 9999.9999, w = 0.0;
        for (int i = 0; i < 27; i++)
        {
            float d = fastdist(vec3(x, y, z), p[i].xyz);
            if (d < m)
            {
                m = d;
                w = p[i].w;
            }
        }
        return vec2(m, w);
    }

    // end of voronoi noise generator part



void main()
{
    // there are gonna be diffrent work groups starting x,y from 0 to 31 and for z only 0

	uint x = gl_GlobalInvocationID.x;
	uint y = gl_GlobalInvocationID.y;
	uint z = gl_GlobalInvocationID.z;

    // texture size's are 32 so every work group with diffrent starting x,y value needs to fill whole tex with data from Perlin noise
    while(x<texSize)
    {
        while(y<texSize)
        {
            while(z<texSize)
            {
                  // Option 1: Generate noise using perling generator
//                float r = perlinNoise(vec3(x/shapeSettings.x, y/shapeSettings.x, z/shapeSettings.x));
//                float g = perlinNoise(vec3(x/shapeSettings.y, y/shapeSettings.y, z/shapeSettings.y));
//                float b = perlinNoise(vec3(x/shapeSettings.z, y/shapeSettings.z, z/shapeSettings.z));
//                float a = perlinNoise(vec3(x/shapeSettings.w, y/shapeSettings.w, z/shapeSettings.w));
//                vec4 res = vec4( (r+1.0f)*0.5f, (g+1.0f)*0.5f, (b+1.0f)*0.5f, (a+1.0f)*0.5f);
                
                // Option 2: Generate noise using voronoi generator
                vec2 r = eval(x/shapeSettings.x, y/shapeSettings.x, z/shapeSettings.x);
                vec2 g = eval(x/shapeSettings.y, y/shapeSettings.y, z/shapeSettings.y);
                vec2 b = eval(x/shapeSettings.z, y/shapeSettings.z, z/shapeSettings.z);
                vec2 a = eval(x/shapeSettings.w, y/shapeSettings.w, z/shapeSettings.w);
                vec4 res = vec4( (1.0f - sqrt(r.x)), (1.0f - sqrt(g.x)), (1.0f - sqrt(b.x)), (1.0f - sqrt(a.x)));
                
                
                imageStore(shape,ivec3(x,y,z),res);

                z +=1;
            }
            z = gl_GlobalInvocationID.z;
            y +=32;     
        }
        y = gl_GlobalInvocationID.y;
        x +=32;
    }

    x = gl_GlobalInvocationID.x;
    y = gl_GlobalInvocationID.y;
    z = gl_GlobalInvocationID.z;

    while(z<texSize)
    {
        // Option 1
//        float r = perlinNoise(vec3(x/detailSettings.x, y/detailSettings.x, z/detailSettings.x));
//        float g = perlinNoise(vec3(x/detailSettings.y, y/detailSettings.y, z/detailSettings.y));
//        float b = perlinNoise(vec3(x/detailSettings.z, y/detailSettings.z, z/detailSettings.z));
//        vec4 res = vec4( (r+1.0f)*0.5f, (g+1.0f)*0.5f, (b+1.0f)*0.5f, 1.0f);

        // Option 2
        vec2 r = eval(x/detailSettings.x, y/detailSettings.x, z/detailSettings.x);
        vec2 g = eval(x/detailSettings.y, y/detailSettings.y, z/detailSettings.y);
        vec2 b = eval(x/detailSettings.z, y/detailSettings.z, z/detailSettings.z);
        vec4 res = vec4( (1.0f - sqrt(r.x)), (1.0f - sqrt(g.x)), (1.0f - sqrt(b.x)), 1.0f);


        imageStore(detail,ivec3(x,y,z),res);
        z+=1;
    }

}