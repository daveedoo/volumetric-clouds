#version 330 core
in vec3 rayDir;
out vec4 FragColor;

uniform vec3 cameraPos;
uniform vec2 windowSize;

uniform vec3 cloudsBoxCenter;
uniform float cloudsBoxSideLength;
uniform float cloudsBoxHeight;

uniform vec2 shapeOffset;
uniform vec2 detailsOffset;

uniform float globalCoverage;
uniform float globalDensity;
uniform vec4 clearColor;

uniform sampler2D cloudsTexture;
uniform sampler3D shapeTexture;
uniform sampler3D detailsTexture;
uniform sampler2D blueNoiseTexture;

uniform vec3 lightPos;
uniform int lightmarchStepCount;
uniform float cloudAbsorption;
uniform float sunAbsorption;
uniform float minLightEnergy;
uniform float densityEps;

uniform sampler2D previousFrame;
uniform bool reprojectionOn;
uniform int reprojIdx;
const int[16] reprojMap = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

uniform float ivo;
uniform float inScatter;
uniform float outScatter;
uniform float sci;
uniform float sce;


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
    //vec4 weather = texture(cloudsTexture, texCoords);
    vec4 weather = vec4(1,1,1,1);
    
    //r, g channels stance for probability of occuring the cloud in given XY coord (3.1.2)
    float WMc = max(weather.r, SAT(globalCoverage - 0.5f) * weather.g * 2);

    //b channel stance for height of cloud (3.1.3.1)
    float wh = weather.b; 
    float ph = 1-height; 
    float SRb = SAT(R(ph, 0.0f, 0.07f, 0.0f, 1.0f));
    float SRt = SAT(R(ph, weather.b * 0.2f, weather.b, 1.0f, 0.0f));
    float SA = SRb * SRt;

    //alfa channel stance for density of cloud (3.1.3.2)
    float wd = weather.a;
    float DRb = ph * SAT(R(ph, 0.0f, 0.15f, 0.0f, 1.0f));
    float DRt = ph * SAT(R(ph, 0.9f, 1.0f, 1.0f, 0.0f));
    float DA = globalDensity * DRb * DRt * weather.a * 2;

    //
    // Shape and detail noise (3.1.4)
    //
    vec4 sn = texture(shapeTexture, vec3(texCoords.x, height, texCoords.y) + vec3(shapeOffset.x, 0,shapeOffset.y));

    float SNsample = R(sn.r, (sn.g * 0.625f + sn.b * 0.25f + sn.a * 0.12f) - 1.0f, 1.0f, 0.0f, 1.0f);

    //detail noise
    vec4 dn = texture(detailsTexture, vec3(texCoords.x, height, texCoords.y) + vec3(detailsOffset.x, 0,detailsOffset.y));

    float DNfbm = dn.r * 0.625f + dn.g * 0.25f + dn.b * 0.125f;
   
    float DNmod = 0.35f * exp(-globalCoverage * 0.75f) * L(DNfbm, 1 - DNfbm, SAT(ph * 5));
    float SNnd = SAT(R(SNsample * SA, 1 - globalCoverage * WMc, 1, 0, 1));
    
    // final result taking everything into consideration
    return SAT(R(SNnd, DNmod, 1, 0, 1)) * DA;
  }

// to use when getCloudValue is fixed
float lightmarchCloud(vec3 pos)
{ 
    vec3 LightDir = normalize(lightPos);

    float dstInBox = testCloudsBoxIntersection(pos, LightDir).y;

    float stepSize = dstInBox/lightmarchStepCount;
    float totalLightDensity =0;

    for(int i=0;i<lightmarchStepCount;i++)
    {
        pos += LightDir*stepSize;

        vec3 boxPoint = cloudsBoxCenter - pos;
        vec2 texCoords = boxPoint.xz/cloudsBoxSideLength + 0.5f;
        float h = boxPoint.y/cloudsBoxHeight + 0.5f;
        float currentDensity = getCloudValue(texCoords,h);

        totalLightDensity += max(0,currentDensity)*stepSize;
    }

    float t = exp(-totalLightDensity*sunAbsorption);

    return minLightEnergy + t*(1-minLightEnergy);
}

const float PI = 3.14159265f;
float HenyeyGreenstein(float phi, float g)
{
    float g2 = g * g;
    
    return 1 / (4 * PI) * (1 - g2) / pow(1 + g2 - 2 * g * cos(phi), 1.5f);
}
float ISextra(float phi)
{
    return sci * pow(SAT(phi), sce);
}

float raymarchCloud(vec3 cameraPos, vec3 rayDir, float dstInBox, float dstToBox, out float t)
{
    float RAYMARCH_STEP = 1.0f;
    //float RAYMARCH_STEP = 1.0f;
    float density = 0.0f;

    // TODO: Blue noise offset of samplePoint
    vec3 samplePoint = cameraPos + dstToBox * rayDir;

    vec3 boxPoint = cloudsBoxCenter - samplePoint;
    vec2 texCoords = boxPoint.xz / cloudsBoxSideLength + 0.5f;

    vec4 blueNoiseValue = texture(blueNoiseTexture, texCoords);
    samplePoint += rayDir * RAYMARCH_STEP * 4 * (blueNoiseValue.r - 0.5);

    float lightEnergy = 0.0f;
    float transmittance1 = 1.0f;


    for (int i = 0; i < dstInBox / RAYMARCH_STEP; i++)
    {
        samplePoint += RAYMARCH_STEP * rayDir;
        vec3 boxPoint = cloudsBoxCenter - samplePoint;
        vec2 texCoords = boxPoint.xz / cloudsBoxSideLength + 0.5f;
        float height = boxPoint.y / cloudsBoxHeight + 0.5f;
        float pointDensity = getCloudValue(texCoords, height);

        if(pointDensity>densityEps)
        {
            float sunviewDot = dot(normalize(lightPos), rayDir);
            float henyeyGreensteinComponent = L(max(HenyeyGreenstein(sunviewDot, inScatter), ISextra(sunviewDot)), HenyeyGreenstein(sunviewDot, -outScatter), ivo);
            lightEnergy += pointDensity * RAYMARCH_STEP * (lightmarchCloud(samplePoint)+henyeyGreensteinComponent) * transmittance1;
            transmittance1 *= exp(-pointDensity*RAYMARCH_STEP*cloudAbsorption);
        }    
    }

    t = transmittance1;
    return lightEnergy;
}

void main()
{
    ivec2 reproj = ivec2(int(gl_FragCoord.x) % 4, int(gl_FragCoord.y) % 4);
    int pixelReprojIdx = reprojMap[reproj.y * 4 + reproj.x];

    if (!reprojectionOn || pixelReprojIdx == reprojIdx)
    {
        vec2 intersection = testCloudsBoxIntersection(cameraPos, rayDir);
        float dstToBox = intersection.x;
        float dstInBox = intersection.y;
        if (dstInBox == 0)
        {
            FragColor = clearColor;
            return;
        }

        float transmittance = 0.0f;

        // ray-marching loop
        float rayMarchedDensity = raymarchCloud(cameraPos, rayDir, dstInBox, dstToBox,transmittance);
        if (rayMarchedDensity < densityEps)
        {
            FragColor = vec4(clearColor.xyz, 0);
        }
        else
        {
            vec3 outCol = vec3(rayMarchedDensity); 
            outCol = outCol + clearColor.xyz*transmittance;
            FragColor = vec4(outCol, 1.0f);
        }
    }
    else
    {
        FragColor = texture(previousFrame, vec2(gl_FragCoord.x / windowSize.x, gl_FragCoord.y / windowSize.y));
    }
}
