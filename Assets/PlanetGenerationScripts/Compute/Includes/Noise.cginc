#include "/FractalNoise.cginc"

// Simplified 3D noise (you can improve this later)
float hash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float noise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float n = i.x + i.y * 57.0 + 113.0 * i.z;
    return lerp(
        lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
             lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
        lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
             lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y),
        f.z);
}

float FractalNoise3D(float3 pos, float4 params[3])
{
    float3 offset = params[0].xyz;
    int numLayers = (int)params[0].w;
    float persistence = params[1].x;
    float lacunarity = params[1].y;
    float scale = params[1].z;
    float elevation = params[1].w;
    
    float value = 0.0;
    float amplitude = 1.0;
    float frequency = scale;
    
    for (int i = 0; i < numLayers; i++)
    {
        float noiseValue = noise3D((pos + offset) * frequency);
        value += noiseValue * amplitude;
        
        frequency *= lacunarity;
        amplitude *= persistence;
    }
    
    return value * elevation;
}