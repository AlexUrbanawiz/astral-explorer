using UnityEngine;
[System.Serializable]
public class PlanetRandomizer : MonoBehaviour
{
    [Header("References")]
    public GPUPlanetGenerator planetGenerator;
    
    [Header("Randomization Settings")]
    [Tooltip("If true, uses a random seed. If false, uses the planet's current seed.")]
    public bool useRandomSeed = true;
    
    [Header("Shape Randomization Ranges")]
    [Tooltip("Range for noise scale")]
    public Vector2 noiseScaleRange = new Vector2(0.5f, 3f);
    
    [Tooltip("Range for number of noise layers")]
    public Vector2Int numLayersRange = new Vector2Int(2, 6);
    
    [Tooltip("Range for persistence")]
    public Vector2 persistenceRange = new Vector2(0.3f, 0.7f);
    
    [Tooltip("Range for lacunarity")]
    public Vector2 lacunarityRange = new Vector2(1.5f, 3f);
    
    [Tooltip("Range for height multiplier")]
    public Vector2 heightMultiplierRange = new Vector2(0.05f, 0.3f);
    
    [Header("Shading Randomization Ranges")]
    [Tooltip("Range for biome noise scale")]
    public Vector2 biomeScaleRange = new Vector2(0.3f, 1.5f);
    
    [Tooltip("Range for biome noise layers")]
    public Vector2Int biomeLayersRange = new Vector2Int(3, 6);
    
    [Tooltip("Range for detail noise scale")]
    public Vector2 detailScaleRange = new Vector2(1f, 4f);
    
    [Tooltip("Range for detail noise layers")]
    public Vector2Int detailLayersRange = new Vector2Int(4, 8);
    
    void Start()
    {
        // Auto-find planet generator if not assigned
        if (planetGenerator == null)
        {
            planetGenerator = GetComponent<GPUPlanetGenerator>();
        }
    }
    
    [ContextMenu("Randomize Shape")]
    public void RandomizeShape()
    {
        if (planetGenerator == null)
        {
            Debug.LogError("No planet generator assigned!");
            return;
        }
        
        // Generate or use random seed
        int newSeed = useRandomSeed ? Random.Range(0, 1000000) : planetGenerator.seed;
        PRNG prng = new PRNG(newSeed);
        
        // Randomize shape parameters
        planetGenerator.seed = newSeed;
        planetGenerator.noiseScale = prng.Range(noiseScaleRange.x, noiseScaleRange.y);
        planetGenerator.numLayers = prng.Range(numLayersRange.x, numLayersRange.y);
        planetGenerator.persistence = prng.Range(persistenceRange.x, persistenceRange.y);
        planetGenerator.lacunarity = prng.Range(lacunarityRange.x, lacunarityRange.y);
        planetGenerator.heightMultiplier = prng.Range(heightMultiplierRange.x, heightMultiplierRange.y);
        
        // Regenerate planet
        planetGenerator.GeneratePlanet();
        
        Debug.Log($"Randomized shape with seed: {newSeed}");
    }
    
    [ContextMenu("Randomize Shading")]
    public void RandomizeShading()
    {
        if (planetGenerator == null)
        {
            Debug.LogError("No planet generator assigned!");
            return;
        }
        
        if (planetGenerator.biomeNoise == null || planetGenerator.detailNoise == null)
        {
            Debug.LogError("Biome or Detail noise settings not assigned! Create them first.");
            return;
        }
        
        // Use planet's seed for consistency
        PRNG prng = new PRNG(planetGenerator.seed);
        
        // Randomize biome noise
        RandomizeNoiseSettings(planetGenerator.biomeNoise, prng, biomeScaleRange, biomeLayersRange);
        
        // Create new PRNG instance for detail (or use same seed for consistency)
        PRNG detailPrng = new PRNG(planetGenerator.seed + 1000); // Offset seed for variation
        RandomizeNoiseSettings(planetGenerator.detailNoise, detailPrng, detailScaleRange, detailLayersRange);
        
        // Regenerate planet to apply shading changes
        planetGenerator.GeneratePlanet();
        
        Debug.Log("Randomized shading noise settings");
    }
    
    [ContextMenu("Randomize Everything")]
    public void RandomizeEverything()
    {
        RandomizeShape();
        RandomizeColors();
        RandomizeShading();
        
    }
    
    void RandomizeNoiseSettings(SimpleNoiseSettings noiseSettings, PRNG prng, Vector2 scaleRange, Vector2Int layersRange)
    {
        if (noiseSettings == null) return;
        
        noiseSettings.scale = prng.Range(scaleRange.x, scaleRange.y);
        noiseSettings.numLayers = prng.Range(layersRange.x, layersRange.y);
        noiseSettings.persistence = prng.Range(0.3f, 0.7f);
        noiseSettings.lacunarity = prng.Range(1.5f, 3f);
        noiseSettings.elevation = prng.Range(0.8f, 1.2f);
        noiseSettings.verticalShift = prng.Range(-0.2f, 0.2f);
        noiseSettings.offset = new Vector3(
            prng.Range(-100f, 100f),
            prng.Range(-100f, 100f),
            prng.Range(-100f, 100f)
        );
    }
    
    [ContextMenu("Randomize Seed Only")]
    public void RandomizeSeed()
    {
        if (planetGenerator == null)
        {
            Debug.LogError("No planet generator assigned!");
            return;
        }
        
        planetGenerator.seed = Random.Range(0, 1000000);
        planetGenerator.GeneratePlanet();
        
        Debug.Log($"New seed: {planetGenerator.seed}");
    }
    [ContextMenu("Randomize Colors")]
    public void RandomizeColors()
    {
        if (planetGenerator == null)
        {
            Debug.LogError("No planet generator assigned!");
            return;
        }
        
        MeshRenderer renderer = planetGenerator.GetComponent<MeshRenderer>();
        if (renderer == null || renderer.sharedMaterial == null)
        {
            Debug.LogError("No material found on planet!");
            return;
        }
        
        Material mat = renderer.sharedMaterial;
        PRNG prng = new PRNG(planetGenerator.seed);
        
        // Randomize colors (adjust property names to match your Shader Graph)
        if (mat.HasProperty("_GrassColor"))
        {
            mat.SetColor("_GrassColor", ColourHelper.Random(prng, 0.3f, 0.7f, 0.6f, 0.9f));
        }
        
        if (mat.HasProperty("_RockColor"))
        {
            mat.SetColor("_RockColor", ColourHelper.Random(prng, 0.2f, 0.5f, 0.3f, 0.6f));
        }
        
        if (mat.HasProperty("_DesertColor"))
        {
            mat.SetColor("_DesertColor", ColourHelper.Random(prng, 0.05f, 0.15f, 0.7f, 0.95f));
        }
        
        Debug.Log("Randomized material colors");
    }
}

