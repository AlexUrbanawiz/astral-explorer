using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
[ExecuteInEditMode]
public class SimplePlanetGenerator : MonoBehaviour
{
    static Dictionary<int, SphereMesh> sphereGenerators;
    
    [Header("Generation Settings")]
    public int resolution = 50;
    public float radius = 1f;

    [Header("Compute Shader")]
    public ComputeShader heightComputeShader;
    
    [Header("Noise Settings")]
    public float heightMultiplier = 0.1f;
    public float noiseScale = 1.0f;
    public int numLayers = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2.0f;

    Mesh mesh;
    Vector3[] baseVerticies;
    Vector3[] modifiedVerticies;
    int[] triangles;  // ADD THIS LINE - store triangles
    
    ComputeBuffer vertexBuffer;
    ComputeBuffer heightBuffer;

    void Start()
    {
        GeneratePlanet();
    }

    void GeneratePlanet()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if(meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = new Mesh();
        }
        mesh = meshFilter.sharedMesh;
        
        mesh.name = "Procedural Planet";
        
        CreateBaseSphere();
        ApplyHeightsGPU();  // Changed from ApplyHeights()
        UpdateMesh();
        
        ReleaseBuffers();  // Clean up after use
    }

    void CreateBaseSphere()
    {
        // Initialize dictionary if needed
        if (sphereGenerators == null)
        {
            sphereGenerators = new Dictionary<int, SphereMesh>();
        }
        
        // Get or create sphere generator for this resolution
        if (!sphereGenerators.ContainsKey(resolution))
        {
            sphereGenerators.Add(resolution, new SphereMesh(resolution));
        }
        var generator = sphereGenerators[resolution];

        // Copy vertices and triangles
        baseVerticies = new Vector3[generator.Vertices.Length];
        triangles = new int[generator.Triangles.Length];
        
        System.Array.Copy(generator.Vertices, baseVerticies, baseVerticies.Length);
        System.Array.Copy(generator.Triangles, triangles, triangles.Length);
        
        // Normalize vertices
        for (int i = 0; i < baseVerticies.Length; i++)
        {
            baseVerticies[i] = baseVerticies[i].normalized;
        }
        
        // Validate triangle indices
        int maxTriangleIndex = 0;
        bool hasInvalidIndices = false;
        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] < 0 || triangles[i] >= baseVerticies.Length)
            {
                Debug.LogError($"Invalid triangle index at {i}: {triangles[i]} (vertex count: {baseVerticies.Length})");
                hasInvalidIndices = true;
            }
            maxTriangleIndex = Mathf.Max(maxTriangleIndex, triangles[i]);
        }
        
        Debug.Log($"Triangle validation - Max index: {maxTriangleIndex}, Vertex count: {baseVerticies.Length}, Triangle count: {triangles.Length / 3}");
        
        if (hasInvalidIndices)
        {
            Debug.LogError("Cannot create mesh - invalid triangle indices found!");
            return;
        }
        
        // Clear mesh
        mesh.Clear();
        
        // Set index format for large meshes
        if (baseVerticies.Length > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        
        // Set vertices FIRST, then triangles
        mesh.vertices = baseVerticies;
        mesh.triangles = triangles;
        
        mesh.RecalculateNormals();
        
        // Debug output
        Debug.Log($"Vertex distances - Min: {baseVerticies[0].magnitude}, Max: {baseVerticies[baseVerticies.Length-1].magnitude}");
        Debug.Log($"Total vertices: {baseVerticies.Length}");
    }

    void ApplyHeightsGPU()
    {
        if(heightComputeShader == null)
        {
            Debug.LogWarning("No compute shader assigned! Please assign SimpleHeight.compute");
            return;
        }
        
        if(baseVerticies == null || baseVerticies.Length == 0)
        {
            Debug.LogError("No base vertices found! Make sure CreateBaseSphere() ran successfully.");
            return;
        }
        
        int vertexCount = baseVerticies.Length;
        
        // Create buffers
        vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        heightBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        
        // Set vertex data
        vertexBuffer.SetData(baseVerticies);
        
        // Set buffers and parameters on compute shader
        int kernelIndex = 0;
        heightComputeShader.SetBuffer(kernelIndex, "vertices", vertexBuffer);
        heightComputeShader.SetBuffer(kernelIndex, "heights", heightBuffer);
        heightComputeShader.SetInt("numVertices", vertexCount);
        heightComputeShader.SetFloat("heightMultiplier", heightMultiplier);
        heightComputeShader.SetFloat("noiseScale", noiseScale);
        heightComputeShader.SetInt("numLayers", numLayers);
        heightComputeShader.SetFloat("persistence", persistence);
        heightComputeShader.SetFloat("lacunarity", lacunarity);
        
        // Dispatch compute shader
        int threadGroupSize = 64;  // Must match [numthreads(64,1,1)] in shader
        int numGroups = Mathf.CeilToInt(vertexCount / (float)threadGroupSize);
        heightComputeShader.Dispatch(kernelIndex, numGroups, 1, 1);
        
        // Get results back from GPU
        float[] heights = new float[vertexCount];
        heightBuffer.GetData(heights);
        // After getting heights, before applying:
        if (heights.Length != baseVerticies.Length)
        {
            Debug.LogError($"Height array length ({heights.Length}) doesn't match vertex count ({baseVerticies.Length})!");
            return;
        }
        // DEBUG: Check noise values
        DebugNoiseValues(heights);
        DebugHeightByPosition(heights);

        // Apply heights to vertices
        modifiedVerticies = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            modifiedVerticies[i] = baseVerticies[i] * radius * heights[i];
        }
        
        Debug.Log($"Applied heights to {vertexCount} vertices using GPU. Radius: {radius}");
    }

    void DebugNoiseValues(float[] heights)
    {
        // Sample noise at different points on sphere
        Vector3[] testPoints = {
            Vector3.up,           // North pole
            Vector3.down,          // South pole
            Vector3.right,         // Equator - right
            Vector3.left,          // Equator - left
            Vector3.forward,       // Equator - front
            Vector3.back           // Equator - back
        };
        
        Debug.Log("=== Noise Value Analysis ===");
        Debug.Log($"Height range - Min: {System.Linq.Enumerable.Min(heights)}, Max: {System.Linq.Enumerable.Max(heights)}, Avg: {System.Linq.Enumerable.Average(heights)}");
        
        // Check if heights are uniform or have patterns
        int lowCount = 0, midCount = 0, highCount = 0;
        float avg = System.Linq.Enumerable.Average(heights);
        float stdDev = 0f;
        
        foreach (float h in heights)
        {
            float diff = h - avg;
            stdDev += diff * diff;
            
            if (h < avg - 0.1f) lowCount++;
            else if (h > avg + 0.1f) highCount++;
            else midCount++;
        }
        stdDev = Mathf.Sqrt(stdDev / heights.Length);
        
        Debug.Log($"Height distribution - Low: {lowCount}, Mid: {midCount}, High: {highCount}");
        Debug.Log($"Standard deviation: {stdDev}");
    }
    void DebugHeightByPosition(float[] heights)
    {
        // Categorize vertices by position (pole vs equator)
        int poleVertices = 0;
        int equatorVertices = 0;
        float poleHeightSum = 0f;
        float equatorHeightSum = 0f;
        
        for (int i = 0; i < baseVerticies.Length; i++)
        {
            Vector3 pos = baseVerticies[i].normalized;
            float latitude = Mathf.Abs(pos.y); // How far from equator (0 = equator, 1 = pole)
            
            if (latitude > 0.9f) // Near poles
            {
                poleVertices++;
                poleHeightSum += heights[i];
            }
            else if (latitude < 0.3f) // Near equator
            {
                equatorVertices++;
                equatorHeightSum += heights[i];
            }
        }
        
        float avgPoleHeight = poleVertices > 0 ? poleHeightSum / poleVertices : 0f;
        float avgEquatorHeight = equatorVertices > 0 ? equatorHeightSum / equatorVertices : 0f;
        
        Debug.Log($"=== Height by Position ===");
        Debug.Log($"Pole vertices: {poleVertices}, Avg height: {avgPoleHeight}");
        Debug.Log($"Equator vertices: {equatorVertices}, Avg height: {avgEquatorHeight}");
        Debug.Log($"Difference: {Mathf.Abs(avgPoleHeight - avgEquatorHeight)}");
    }

    void UpdateMesh()
    {
        if (modifiedVerticies == null || modifiedVerticies.Length == 0)
        {
            Debug.LogError("No modified vertices to apply! Make sure ApplyHeightsGPU() ran successfully.");
            return;
        }
        
        if (triangles == null || triangles.Length == 0)
        {
            Debug.LogError("No triangles found! Make sure CreateBaseSphere() ran successfully.");
            return;
        }
        
        mesh.vertices = modifiedVerticies;
        mesh.triangles = triangles;  // IMPORTANT: Reapply triangles
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
    }
    
    void ReleaseBuffers()
    {
        if (vertexBuffer != null)
        {
            vertexBuffer.Release();
            vertexBuffer = null;
        }
        if (heightBuffer != null)
        {
            heightBuffer.Release();
            heightBuffer = null;
        }
    }

    [ContextMenu("Regenerate Planet")]
    void Regenerate()
    {
        GeneratePlanet();
    }

    void OnValidate()
    {
        GeneratePlanet();
    }
    
    void OnDestroy()
    {
        ReleaseBuffers();
    }

    [ContextMenu("Performance Test")]
    void PerfomanceTest()
    {
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        int[] testResolutions = { 10, 50, 100, 200 };
        foreach (int res in testResolutions)
        {
            resolution = res;
            sw.Restart();
            GeneratePlanet();
            sw.Stop();
            
            int vertexCount = baseVerticies.Length;
            Debug.Log($"Resolution: {res}, Vertices: {vertexCount}, Time: {sw.ElapsedMilliseconds}ms");
        }
    }
}
