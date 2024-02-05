#version 330 core
in vec3 rayDir;
out vec4 FragColor;

uniform vec3 cameraPos;

uniform vec3 cloudsBoxCenter;
uniform float cloudsBoxSideLength;
uniform float cloudsBoxHeight;

uniform vec2 shapeOffset;
uniform vec2 detailsOffset;

uniform float globalCoverage;
uniform float globalDensity;

uniform sampler2D cloudsTexture;

uniform sampler3D shapeTexture;
uniform sampler3D detailsTexture;
//uniform sampler2D weatherTexture;


// Returns (distanceToBox, distanceInBox). If ray misses box, distanceInBox will be zero
vec2 testCloudsBoxIntersection(vec3 rayOrigin, vec3 raydir)
{
    // https://github.com/SebLague/Clouds/blob/master/Assets/Scripts/Clouds/Shaders/Clouds.shader#L121
    vec3 offset = vec3(cloudsBoxSideLength / 2, cloudsBoxHeight / 2, cloudsBoxSideLength / 2);;
    vec3 cloudsBoxMin = cloudsBoxCenter - offset;
    vec3 cloudsBoxMax = cloudsBoxCenter + offset;

    vec3 t0 = (cloudsBoxMin - rayOrigin) / raydir;
    vec3 t1 = (cloudsBoxMax - rayOrigin) / raydir;
    vec3 tmin = min(t0, t1);
    vec3 tmax = max(t0, t1);
                
    float dstA = max(max(tmin.x, tmin.y), tmin.z);
    float dstB = min(tmax.x, min(tmax.y, tmax.z));

    // CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
    // dstA is dst to nearest intersection, dstB dst to far intersection

    // CASE 2: ray intersects box from inside (dstA < 0 < dstB)
    // dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

    // CASE 3: ray misses box (dstA > dstB)

    float distanceToBox = max(0, dstA);
    float distanceInBox = max(0, dstB - distanceToBox);
    return vec2(distanceToBox, distanceInBox);
}

//
// helping functions from page 9:
// (used in getCLoud() function
//
float R(float v, float l0, float h0, float ln, float hn) { return ln + (((v - l0) * (hn - ln))/(h0 - l0)); }
float SAT(float v) { return clamp(v, 0, 1); }
float L(float v0, float v1, float ival) { return (1 - ival) * v0 + ival * v1; }

//
// all values need to be within [0, 1]
//
float getCloudValue(vec2 texCoords, float height)
{
    vec4 weather = texture(cloudsTexture, texCoords);
    
    //r, g channels stance for probability of occuring the cloud in given XY coord (3.1.2)
    float WMc = max(weather.r, SAT(globalCoverage - 0.5f) * weather.g * 2);

    //b channel stance for height of cloud (3.1.3.1)
    float wh = weather.b; 
    float ph = 1 - height; 
    float SRb = SAT(R(ph, 0.0f, 0.07f, 0.0f, 1.0f));
    float SRt = SAT(R(ph, weather.b * 0.2f, weather.b, 1.0f, 0.0f));
    float SA = SRb * SRt;

    //alfa channel stance for density of cloud (3.1.3.2)
    float wd = weather.a;
    float daph = height;
    float DRb = daph * SAT(R(daph, 0.0f, 0.15f, 0.0f, 1.0f));
    float DRt = daph * SAT(R(daph, 0.9f, 1.0f, 1.0f, 0.0f));
    float DA = globalDensity * DRb * DRt * wd * 2;

    //
    // Shape and detail noise (3.1.4)
    //
    vec4 sn = texture(shapeTexture, vec3(texCoords + shapeOffset, 0)); 

    float SNsample = R(sn.r, (sn.g * 0.625f + sn.b * 0.25f + sn.a * 0.12f) - 1.0f, 1.0f, 0.0f, 1.0f);
    float SN = SAT(R(SNsample * SA, 1 - globalCoverage * WMc, 1.0f, 0.0f, 1.0f)) * DA;
    
    //detail noise
    vec4 dn = texture(detailsTexture, vec3(texCoords + detailsOffset, 0));

    float DNfbm = dn.r * 0.625f + dn.g * 0.25f + dn.b * 0.125f; 
    float DNmod = 0.35f * exp(-globalCoverage * 0.75f) * L(DNfbm, 1 - DNfbm, SAT(ph * 5));
    float SNnd = SAT(R(SNsample * SA, 1 - globalCoverage * WMc, 1.0f, 0.0f, 1.0f));
    
    // final result taking everything into consideration
    float result = SAT(R(SNnd, DNmod, 1, 0, 1)) * DA;
    //return result;
    // result giving good effects, but it is not final one
    return result;
}


float raymarchCloud(vec3 cameraPos, vec3 rayDir, float dstInBox, float dstToBox)
{
    float RAYMARCH_STEP = 0.01f;
    //float RAYMARCH_STEP = 1.0f;
    float density = 0.0f;
    vec3 samplePoint = cameraPos + dstToBox * rayDir;
    for (int i = 0; i < dstInBox / RAYMARCH_STEP; i++)
    {
        samplePoint += RAYMARCH_STEP * rayDir;

        vec3 boxPoint = cloudsBoxCenter - samplePoint;
        vec2 texCoords = boxPoint.xz / cloudsBoxSideLength + 0.5f;
        float height = boxPoint.y / cloudsBoxHeight + 0.5f;

        float cloud = getCloudValue(texCoords, height);

        // TODO: what is the best coefficient? (in place of RAYMARCH_STEP here)
        density += RAYMARCH_STEP * cloud;
    }
    return density;
}



void main()
{
    vec2 intersection = testCloudsBoxIntersection(cameraPos, rayDir);
    float dstToBox = intersection.x;
    float dstInBox = intersection.y;
    if (dstInBox == 0)
    {
        discard;
    }

    // ray-marching loop
    float rayMarchedDensity = raymarchCloud(cameraPos, rayDir, dstInBox, dstToBox);

    FragColor = vec4(rayMarchedDensity, rayMarchedDensity, rayMarchedDensity, 1.0f);
}