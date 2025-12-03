using UnityEngine;

[ExecuteInEditMode]
public class SimpleHeightGenerator : MonoBehaviour
{
    // [Header("Noise Settings")]
    // public int seed = 0;
    // public float scale = 1f;
    // public int numLayers = 4;
    // public float persistence = .5f;
    // public float lacunarity = 2f;

    // [Header("Height Settings")]
    // public float heightMultiplier = .1f;

    // float Hash(Vector3 p)
    // {
    //     p = new Vector3(
    //         Mathf.Repeat(p.x * 0.3183099f + 0.1f, 1f),
    //         Mathf.Repeat(p.y * 0.3183099f + 0.1f, 1f),
    //         Mathf.Repeat(p.z * 0.3183099f + 0.1f, 1f)
    //     );
    //     p = new Vector3(p.x * 17f, p.y * 17f, p.z * 17f);
    //     return Mathf.Repeat(p.x * p.y * p.z * (p.x + p.y + p.z), 1f);
    // }

    // float Noise3D(Vector3 p)
    // {
    //     Vector3 i = new Vector3(Mathf.Floor(p.x), Mathf.Floor(p.y), Mathf.Floor(p.z));
    //     Vector3 f = new Vector3(p.x - i.x, p.y - i.y, p.z - i.z);
        
    //     // Smooth interpolation
    //     f = new Vector3(
    //         f.x * f.x * (3f - 2f * f.x),
    //         f.y * f.y * (3f - 2f * f.y),
    //         f.z * f.z * (3f - 2f * f.z)
    //     );
        
    //     float n = i.x + i.y * 57f + i.z * 113f;
        
    //     return Mathf.Lerp(
    //         Mathf.Lerp(
    //             Mathf.Lerp(Hash(new Vector3(n + 0f, 0, 0)), Hash(new Vector3(n + 1f, 0, 0)), f.x),
    //             Mathf.Lerp(Hash(new Vector3(n + 57f, 0, 0)), Hash(new Vector3(n + 58f, 0, 0)), f.x),
    //             f.y
    //         ),
    //         Mathf.Lerp(
    //             Mathf.Lerp(Hash(new Vector3(n + 113f, 0, 0)), Hash(new Vector3(n + 114f, 0, 0)), f.x),
    //             Mathf.Lerp(Hash(new Vector3(n + 170f, 0, 0)), Hash(new Vector3(n + 171f, 0, 0)), f.x),
    //             f.y
    //         ),
    //         f.z
    //     );
    // }

    // float FractalNoise(Vector3 position)
    // {
    //     float value = 0f;
    //     float amplitude = 1f;
    //     float frequency = scale;

    //     for (int i = 0; i < numLayers; i++)
    //     {
    //         float noiseValue = Noise3D(position, frequency);
    //         value += noiseValue * amplitude;
    //         frequency *= lacunarity;
    //         amplitude *= persistence;
    //     }
    //     return value;
    // }

    // public float CalculateHeight(Vector3 pointOnUnitSphere)
    // {
    //     float noiseValue = FractalNoise(pointOnUnitSphere);
    //     float height = 1f + (noiseValue - .5f) * heightMultiplier;
    //     return height;
    // }


    // [ContextMenu("Test Height Calculation")]
    // void TestHeightCalculation()
    // {
    //     Vector3 testPoint = Vector3.up;
    //     float height = CalculateHeight(testPoint);
    //     Debug.Log($"Point: {testPoint}, Height: {height}");

    //     for (int i = 0; i < 10; i++)
    //     {
    //         Vector3 randomPoint = Random.onUnitSphere;
    //         height = CalculateHeight(randomPoint);
    //         Debug.Log($"Random point {i}: {randomPoint}, Height: {height}");
    //     }
    // }
}





