using UnityEngine;

[CreateAssetMenu(menuName = "Planet/Shading/Earth-Like")]
public class EarthLikeShading : PlanetShading
{
    [Header("Shading Data")]
    public SimpleNoiseSettings detailWarpNoise;
    public SimpleNoiseSettings detailNoise;
    public SimpleNoiseSettings largeNoise;
    public SimpleNoiseSettings smallNoise;
    
    protected override void SetShadingDataComputeProperties()
    {
        PRNG random = new PRNG(seed);
        detailNoise.SetComputeValues(shadingDataCompute, random, "_detail");
        detailWarpNoise.SetComputeValues(shadingDataCompute, random, "_detailWarp");
        largeNoise.SetComputeValues(shadingDataCompute, random, "_large");
        smallNoise.SetComputeValues(shadingDataCompute, random, "_small");
    }
}
