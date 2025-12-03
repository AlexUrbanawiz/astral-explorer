using UnityEngine;

[CreateAssetMenu(menuName = "Planet/Shape/Moon-Like")]
public class MoonLikeShape : PlanetShape
{
    [Header("Noise Settings")]
    public SimpleNoiseSettings shapeNoise;
    public RidgeNoiseSettings ridgeNoise;
    public RidgeNoiseSettings ridgeNoise2;
    // public CraterSettings craterSettings;
    
    protected override void SetShapeData()
    {
        var prng = new PRNG(seed);
        shapeNoise.SetComputeValues(heightMapCompute, prng, "_shape");
        ridgeNoise.SetComputeValues(heightMapCompute, prng, "_ridge");
        ridgeNoise2.SetComputeValues(heightMapCompute, prng, "_ridge2");
        // craterSettings.SetComputeValues(heightMapCompute, seed);
    }
    
    public override void ReleaseBuffers()
    {
        base.ReleaseBuffers();
        // craterSettings.ReleaseBuffers();
    }
}
