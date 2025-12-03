using UnityEngine;

[CreateAssetMenu(menuName = "Planet/Shape/Earth-Like")]
public class EarthLikeShape : PlanetShape
{
    [Header("Continent Settings")]
    public float oceanDepthMultiplier = 5f;
    public float oceanFloorDepth = 1.5f;
    public float oceanFloorSmoothing = 0.5f;
    public float mountainBlend = 1.2f;
    
    [Header("Noise Settings")]
    public SimpleNoiseSettings continentNoise;
    public SimpleNoiseSettings maskNoise;
    public RidgeNoiseSettings ridgeNoise;
    
    protected override void SetShapeData()
    {
        var prng = new PRNG(seed);
        continentNoise.SetComputeValues(heightMapCompute, prng, "_continents");
        ridgeNoise.SetComputeValues(heightMapCompute, prng, "_mountains");
        maskNoise.SetComputeValues(heightMapCompute, prng, "_mask");
        
        heightMapCompute.SetFloat("oceanDepthMultiplier", oceanDepthMultiplier);
        heightMapCompute.SetFloat("oceanFloorDepth", oceanFloorDepth);
        heightMapCompute.SetFloat("oceanFloorSmoothing", oceanFloorSmoothing);
        heightMapCompute.SetFloat("mountainBlend", mountainBlend);
    }
}
