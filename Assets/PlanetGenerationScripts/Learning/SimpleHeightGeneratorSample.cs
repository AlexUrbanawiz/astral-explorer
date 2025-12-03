using UnityEngine;

/// <summary>
/// Lesson 2.1: Simple CPU-based height generator for learning procedural generation.
/// This demonstrates how noise functions create terrain heights before moving to GPU.
/// </summary>
public class SimpleHeightGeneratorSample : MonoBehaviour
{
    [Header("Noise Settings")]
    [Tooltip("Seed for random number generation. Same seed = same result.")]
    public int seed = 0;

    [Tooltip("Overall scale of noise features. Smaller = larger features.")]
    public float scale = 1f;

    [Tooltip("Number of noise layers (octaves). More layers = more detail.")]
    [Range(1, 8)]
    public int numLayers = 4;

    [Tooltip("How much each layer contributes. Lower = smoother.")]
    [Range(0.1f, 1f)]
    public float persistence = 0.5f;

    [Tooltip("How much each layer is scaled. Usually 2.0 (double size).")]
    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [Header("Height Settings")]
    [Tooltip("How much the noise affects terrain height.")]
    public float heightMultiplier = 0.1f;

    // Simple noise function using Unity's built-in PerlinNoise
    // This is 2D noise - for learning purposes
    float SimpleNoise(Vector3 position, float noiseScale)
    {
        // PerlinNoise takes 2D coordinates, so we use X and Z
        float x = position.x * noiseScale;
        float z = position.z * noiseScale;
        return Mathf.PerlinNoise(x, z);
    }

    /// <summary>
    /// Fractal noise: multiple layers of noise combined.
    /// Each layer is smaller (higher frequency) and contributes less (lower amplitude).
    /// </summary>
    float FractalNoise(Vector3 position)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = scale;

        for (int i = 0; i < numLayers; i++)
        {
            // Get noise value at this layer
            float noiseValue = SimpleNoise(position, frequency);

            // Add to total (weighted by amplitude)
            value += noiseValue * amplitude;

            // Next layer: higher frequency, lower amplitude
            frequency *= lacunarity;  // Double the frequency (usually)
            amplitude *= persistence;  // Half the amplitude (usually)
        }

        return value;
    }

    /// <summary>
    /// Calculate height for a point on unit sphere.
    /// Returns a height multiplier (1.0 = no change, 1.1 = 10% taller).
    /// </summary>
    public float CalculateHeight(Vector3 pointOnUnitSphere)
    {
        // Get noise value (0 to 1 range)
        float noiseValue = FractalNoise(pointOnUnitSphere);

        // Convert to height (centered around 1.0, with variation)
        // noiseValue is 0-1, so (noiseValue - 0.5) is -0.5 to 0.5
        float height = 1f + (noiseValue - 0.5f) * heightMultiplier;

        return height;
    }

    /// <summary>
    /// Test function - call this from Inspector button or Start()
    /// </summary>
    [ContextMenu("Test Height Calculation")]
    void TestHeightCalculation()
    {
        Vector3 testPoint = Vector3.up; // Point at top of sphere
        float height = CalculateHeight(testPoint);
        Debug.Log($"Point: {testPoint}, Height: {height}");

        // Test multiple points
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = Random.onUnitSphere;
            height = CalculateHeight(randomPoint);
            Debug.Log($"Random point {i}: {randomPoint}, Height: {height}");
        }
    }

    /// <summary>
    /// Visualize each noise layer separately for learning.
    /// </summary>
    [ContextMenu("Visualize Noise Layers")]
    void VisualizeLayers()
    {
        Vector3 testPoint = Vector3.up;
        Debug.Log("=== Noise Layer Breakdown ===");

        float totalValue = 0f;
        float amplitude = 1f;
        float frequency = scale;

        for (int layer = 0; layer < numLayers; layer++)
        {
            float noiseValue = SimpleNoise(testPoint, frequency);
            float contribution = noiseValue * amplitude;
            totalValue += contribution;

            Debug.Log($"Layer {layer}: Frequency={frequency:F2}, Amplitude={amplitude:F2}, " +
                     $"Noise={noiseValue:F3}, Contribution={contribution:F3}");

            frequency *= lacunarity;
            amplitude *= persistence;
        }

        Debug.Log($"Total Value: {totalValue:F3}");
    }
}

