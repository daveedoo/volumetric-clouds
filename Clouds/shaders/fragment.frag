#version 330 core
in vec3 rayDir;
out vec4 FragColor;

uniform vec3 cameraPos;

uniform vec3 cloudsBoxCenter;
uniform float cloudsBoxSideLength;
uniform float cloudsBoxHeight;


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

void main()
{
    vec2 intersection = testCloudsBoxIntersection(cameraPos, rayDir);
    float dstToBox = intersection.x;
    float dstInBox = intersection.y;

    if (dstInBox == 0)
    {
        discard;
    }

//    vec3 rayMarchStartPoint = cameraPos + dstToBox*rayDir;



    FragColor = vec4(dstInBox, dstInBox, dstInBox, 1.0f);
//    FragColor = vec4(rayMarchStartPoint, 1.0f);
}
