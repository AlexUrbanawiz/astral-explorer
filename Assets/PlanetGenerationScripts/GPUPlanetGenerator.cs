using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteInEditMode]
public class GPUPlanetGenerator : MonoBehaviour
{
    static Dictionary<int, SphereMesh> sphereGenerators;
    
    [Header("Generation")]
    public int resolution = 100;
    public float radius = 1f;
    
    [Header("Compute Shader")]
    public ComputeShader heightCompute;
    
    [Header("Noise Settings")]
    public int seed = 0;
    public float noiseScale = 1f;
    public int numLayers = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float heightMultiplier = 0.1f;

    [Header("Terrain Features")]
    [Tooltip("Enable continent/ocean system (Earth-like)")]
    public bool useContinents = false;

    [Tooltip("Enable mountain ridges")]
    public bool useMountains = false;

    [Tooltip("Enable craters (Moon-like)")]
    public bool useCraters = false;

    [Header("Continent Settings")]
    public SimpleNoiseSettings continentNoise;
    public SimpleNoiseSettings continentMaskNoise;
    public float oceanDepthMultiplier = 5f;
    public float oceanFloorDepth = 1.5f;
    public float oceanFloorSmoothing = 0.5f;
    public float mountainBlend = 1.2f;

    [Header("Mountain Settings")]
    public RidgeNoiseSettings mountainNoise;

    [Header("Shading")]
    public ComputeShader shadingComputeShader;
    [Header("Shading Noise Settings")]
    public SimpleNoiseSettings biomeNoise;
    public SimpleNoiseSettings detailNoise;

    [HideInInspector]
    public int vertexCount;
    Vector4[] shadingData;
    
    Mesh mesh;
    Vector3[] baseVertices;
    Vector3[] modifiedVertices;
    int[] triangles;
    float[] heights; // Store heights from GPU
    
    ComputeBuffer vertexBuffer;
    ComputeBuffer heightBuffer;
    

    [Header("LOD Settings")]
    public LODSettings lodSettings;
    Mesh[] lodMeshes;

    [Header("Collision")]
    [Tooltip("Resolution for collision mesh (lower = better performance)")]
    public int collisionResolution = 30;
    Mesh collisionMesh;


    void GenerateLODMeshes()
    {
        if (lodSettings == null)
        {
            Debug.LogWarning("LOD Settings not assigned!");
            return;
        }
        
        lodMeshes = new Mesh[3];
        for (int i = 0; i < 3; i++)
        {
            int res = lodSettings.GetLODResolution(i);
            GenerateMeshAtResolution(res, ref lodMeshes[i]);
        }
    }

    public void SetLOD(int lodLevel)
    {
        if (lodMeshes != null && lodLevel >= 0 && lodLevel < lodMeshes.Length)
        {
            GetComponent<MeshFilter>().sharedMesh = lodMeshes[lodLevel];
        }
    }
    
    // Generate a mesh at a specific resolution and output to the provided mesh reference
    void GenerateMeshAtResolution(int targetResolution, ref Mesh outputMesh)
    {
        if (outputMesh == null)
        {
            outputMesh = new Mesh();
        }
        outputMesh.Clear();
        outputMesh.name = $"GPU Planet LOD (Res: {targetResolution})";
        
        // Store original resolution
        int originalResolution = resolution;
        
        // Temporarily set resolution for generation
        resolution = targetResolution;
        
        // Generate base sphere at target resolution
        Vector3[] tempBaseVertices;
        int[] tempTriangles;
        CreateBaseSphereAtResolution(targetResolution, out tempBaseVertices, out tempTriangles);
        
        // Calculate heights on GPU
        float[] tempHeights = CalculateHeightsGPUForVertices(tempBaseVertices);
        
        // Apply heights
        Vector3[] tempModifiedVertices = new Vector3[tempBaseVertices.Length];
        for (int i = 0; i < tempBaseVertices.Length; i++)
        {
            tempModifiedVertices[i] = tempBaseVertices[i] * radius * tempHeights[i];
        }
        
        // Set index format for large meshes
        if (tempBaseVertices.Length > 65535)
        {
            outputMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        
        // Update output mesh
        outputMesh.vertices = tempModifiedVertices;
        outputMesh.triangles = tempTriangles;
        
        // Generate UVs
        Vector2[] uvs = new Vector2[tempModifiedVertices.Length];
        for (int i = 0; i < tempModifiedVertices.Length; i++)
        {
            Vector3 v = tempModifiedVertices[i].normalized;
            float u = Mathf.Atan2(v.x, v.z) / (2f * Mathf.PI) + 0.5f;
            float v_coord = Mathf.Asin(v.y) / Mathf.PI + 0.5f;
            uvs[i] = new Vector2(u, v_coord);
        }
        outputMesh.uv = uvs;
        
        outputMesh.RecalculateNormals();
        outputMesh.RecalculateTangents();
        outputMesh.RecalculateBounds();
        
        // Restore original resolution
        resolution = originalResolution;
    }
    
    // Helper method to create base sphere at specific resolution
    void CreateBaseSphereAtResolution(int targetResolution, out Vector3[] vertices, out int[] triangles)
    {
        // Initialize dictionary if needed
        if (sphereGenerators == null)
        {
            sphereGenerators = new Dictionary<int, SphereMesh>();
        }
        
        // Get or create sphere generator for target resolution
        if (!sphereGenerators.ContainsKey(targetResolution))
        {
            sphereGenerators.Add(targetResolution, new SphereMesh(targetResolution));
        }
        var generator = sphereGenerators[targetResolution];
        
        // Copy vertices and triangles
        vertices = new Vector3[generator.Vertices.Length];
        triangles = new int[generator.Triangles.Length];
        
        System.Array.Copy(generator.Vertices, vertices, vertices.Length);
        System.Array.Copy(generator.Triangles, triangles, triangles.Length);
        
        // Normalize all vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = vertices[i].normalized;
        }
    }
    
    // Helper method to calculate heights for specific vertices
    float[] CalculateHeightsGPUForVertices(Vector3[] verticesToProcess)
    {
        if (heightCompute == null)
        {
            Debug.LogError("No compute shader assigned!");
            return new float[0];
        }
        
        int vertexCount = verticesToProcess.Length;
        int kernelIndex = heightCompute.FindKernel("CSMain");
        
        // Create temporary buffers
        ComputeBuffer tempVertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        ComputeBuffer tempHeightBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        
        // Set vertex data
        tempVertexBuffer.SetData(verticesToProcess);
        
        // Initialize PRNG
        PRNG prng = new PRNG(seed);
        
        // --- SET NOISE PARAMETERS (MATCHING PLANET RANDOMIZER) ---
        
        // 1. Base Noise & Warp (Using public fields)
        if (continentNoise != null)
        {
            continentNoise.SetComputeValues(heightCompute, prng, "_base", noiseScale, heightMultiplier, persistence);
             // Reset PRNG for warp to match sequence (optional but safer for consistency)
             prng = new PRNG(seed); 
             continentNoise.SetComputeValues(heightCompute, prng, "_warp", noiseScale, heightMultiplier, persistence);
        }
        // 2. Continents
        if (useContinents && continentNoise != null)
        {
            continentNoise.SetComputeValues(heightCompute, prng, "_continents");
            if (continentMaskNoise != null)
                continentMaskNoise.SetComputeValues(heightCompute, prng, "_mask");
        }
        // 3. Mountains
        if (useMountains && mountainNoise != null)
        {
            mountainNoise.SetComputeValues(heightCompute, prng, "_mountains");
        }
        // 4. Set Feature Toggles & Floats
        heightCompute.SetFloat("heightMultiplier", heightMultiplier);
        heightCompute.SetInt("seed", seed);
        heightCompute.SetFloat("useContinents", useContinents ? 1.0f : 0.0f);
        heightCompute.SetFloat("useMountains", useMountains ? 1.0f : 0.0f);
        heightCompute.SetFloat("useCraters", useCraters ? 1.0f : 0.0f);
        heightCompute.SetFloat("oceanDepthMultiplier", oceanDepthMultiplier);
        heightCompute.SetFloat("oceanFloorDepth", oceanFloorDepth);
        heightCompute.SetFloat("oceanFloorSmoothing", oceanFloorSmoothing);
        heightCompute.SetFloat("mountainBlend", mountainBlend);
        // ---------------------------------------------------------
        
        // Set buffers
        heightCompute.SetBuffer(kernelIndex, "vertices", tempVertexBuffer);
        heightCompute.SetBuffer(kernelIndex, "heights", tempHeightBuffer);
        heightCompute.SetInt("numVertices", vertexCount);
        
        // Dispatch compute shader
        int threadGroupSize = 64;
        int numGroups = Mathf.CeilToInt(vertexCount / (float)threadGroupSize);
        heightCompute.Dispatch(kernelIndex, numGroups, 1, 1);
        
        // Get results
        float[] resultHeights = new float[vertexCount];
        tempHeightBuffer.GetData(resultHeights);
        
        // Clean up temporary buffers
        tempVertexBuffer.Release();
        tempHeightBuffer.Release();
        
        return resultHeights;
    }


    void Start()
    {
        GeneratePlanet();
    }
    
    public void GeneratePlanet()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = new Mesh();
        }
        mesh = meshFilter.sharedMesh;
        mesh.name = "GPU Generated Planet";

        CreateBaseSphere();
        
        vertexCount = mesh.vertices.Length;

        Vector3[] initialVertices = mesh.vertices;
        // 1. Release old buffers BEFORE creating new ones
        ReleaseBuffers();

        // 2. Create the Compute Buffers (THIS IS THE MISSING STEP)

        // Create the vertex buffer (Vector3 is 3 floats)
        // sizeof(float) * 3 = 12 bytes
        vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        vertexBuffer.SetData(initialVertices); // Populate the buffer with the initial sphere vertices

        // Create the height buffer (float is 1 float)
        // sizeof(float) = 4 bytes
        heightBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        // Create base sphere
        
        
        // Calculate heights on GPU
        CalculateHeightsGPU();
        
        // Apply heights
        ApplyHeights();
        
        UpdateMesh();
        
        // Generate shading data and store in UVs
        GenerateShadingData();

        GenerateCollisionMesh();
        
        ReleaseBuffers();
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
        baseVertices = new Vector3[generator.Vertices.Length];
        triangles = new int[generator.Triangles.Length];
        
        System.Array.Copy(generator.Vertices, baseVertices, baseVertices.Length);
        System.Array.Copy(generator.Triangles, triangles, triangles.Length);
        
        // Normalize all vertices to ensure perfect unit sphere
        for (int i = 0; i < baseVertices.Length; i++)
        {
            baseVertices[i] = baseVertices[i].normalized;
        }
        
        // Validate triangle indices
        int maxTriangleIndex = 0;
        bool hasInvalidIndices = false;
        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] < 0 || triangles[i] >= baseVertices.Length)
            {
                Debug.LogError($"Invalid triangle index at {i}: {triangles[i]} (vertex count: {baseVertices.Length})");
                hasInvalidIndices = true;
            }
            maxTriangleIndex = Mathf.Max(maxTriangleIndex, triangles[i]);
        }
        
        if (hasInvalidIndices)
        {
            Debug.LogError("Cannot create mesh - invalid triangle indices found!");
            return;
        }
        
        // Clear mesh
        mesh.Clear();
        
        // Set index format for large meshes
        const int vertexLimit16Bit = 65535;
        if (baseVertices.Length > vertexLimit16Bit)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        
        // Set vertices first, then triangles
        mesh.vertices = baseVertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }       
    
    void CalculateHeightsGPU()
    {
        // Make sure the Compute Shader asset is assigned!
        if (heightCompute == null)
        {
            Debug.LogError("Height Compute Shader is not assigned on the GPUPlanetGenerator!");
            return;
        }

        // 1. FIX: Explicitly find the kernel (Fixes "Kernel at index (0) is invalid")
        int kernelIndex = heightCompute.FindKernel("CSMain"); 

        if (kernelIndex < 0)
        {
            Debug.LogError("Failed to find kernel 'CSMain' in PlanetHeight.compute! Check the spelling.");
            // Assuming ReleaseBuffers() is called elsewhere or at cleanup
            return;
        }

        // 2. Set necessary single float/int properties directly on the Compute Shader
        // (The noise *arrays* are set by SetComputeValues in PlanetRandomizer:RandomizeShape())

        // Base properties
        heightCompute.SetFloat("heightMultiplier", heightMultiplier);
        heightCompute.SetInt("seed", seed); 
        
        // Feature toggles
        heightCompute.SetFloat("useContinents", useContinents ? 1.0f : 0.0f);
        heightCompute.SetFloat("useMountains", useMountains ? 1.0f : 0.0f);
        heightCompute.SetFloat("useCraters", useCraters ? 1.0f : 0.0f);
        
        // Continent floats (set regardless of the toggle, to ensure the shader has values)
        heightCompute.SetFloat("oceanDepthMultiplier", oceanDepthMultiplier);
        heightCompute.SetFloat("oceanFloorDepth", oceanFloorDepth);
        heightCompute.SetFloat("oceanFloorSmoothing", oceanFloorSmoothing);
        heightCompute.SetFloat("mountainBlend", mountainBlend);
        

        // 3. Set buffers for the kernel
        heightCompute.SetBuffer(kernelIndex, "vertices", vertexBuffer);
        heightCompute.SetBuffer(kernelIndex, "heights", heightBuffer);
        heightCompute.SetInt("numVertices", vertexCount); 
        
        // Dispatch compute shader
        int threadGroupSize = 64; 
        int numGroups = Mathf.CeilToInt(vertexCount / (float)threadGroupSize);
        heightCompute.Dispatch(kernelIndex, numGroups, 1, 1);
        
        // Get results back from GPU
        heights = new float[vertexCount];
        heightBuffer.GetData(heights);
        
        Debug.Log($"Calculated {heights.Length} heights on GPU. Min: {System.Linq.Enumerable.Min(heights)}, Max: {System.Linq.Enumerable.Max(heights)}");
    }
    
    void ApplyHeights()
    {
        if (heights == null || heights.Length == 0)
        {
            Debug.LogError("No heights calculated! Make sure CalculateHeightsGPU() ran successfully.");
            return;
        }
        
        if (baseVertices == null || baseVertices.Length != heights.Length)
        {
            Debug.LogError($"Vertex count ({baseVertices?.Length ?? 0}) doesn't match height count ({heights.Length})!");
            return;
        }
        
        // Apply heights to vertices
        modifiedVertices = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
        {
            // Multiply base vertex by radius and height to get final position
            modifiedVertices[i] = baseVertices[i] * radius * heights[i];
        }
        
        Debug.Log($"Applied heights to {modifiedVertices.Length} vertices.");
    }
    
    void UpdateMesh()
    {
        if (modifiedVertices == null || modifiedVertices.Length == 0)
        {
            Debug.LogError("No modified vertices to apply! Make sure ApplyHeights() ran successfully.");
            return;
        }
        
        if (triangles == null || triangles.Length == 0)
        {
            Debug.LogError("No triangles found! Make sure CreateBaseSphere() ran successfully.");
            return;
        }
        
        // Update mesh with modified vertices
        mesh.vertices = modifiedVertices;
        mesh.triangles = triangles; // Reapply triangles
        
        // Generate UVs for proper rendering
        Vector2[] uvs = new Vector2[modifiedVertices.Length];
        for (int i = 0; i < modifiedVertices.Length; i++)
        {
            Vector3 v = modifiedVertices[i].normalized;
            // Spherical UV mapping
            float u = Mathf.Atan2(v.x, v.z) / (2f * Mathf.PI) + 0.5f;
            float v_coord = Mathf.Asin(v.y) / Mathf.PI + 0.5f;
            uvs[i] = new Vector2(u, v_coord);
        }
        mesh.uv = uvs;
        
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
    }

    void GenerateShadingData()
    {
        if (shadingComputeShader == null)
        {
            Debug.LogWarning("No shading compute shader assigned!");
            return;
        }
        
        if (modifiedVertices == null || modifiedVertices.Length == 0)
        {
            Debug.LogError("No vertices found! Generate planet first.");
            return;
        }
        
        int vertexCount = modifiedVertices.Length;  
        
        // Get normals from mesh (needed for slope calculation)
        Vector3[] normals = mesh.normals;
        if (normals == null || normals.Length != vertexCount)
        {
            mesh.RecalculateNormals();
            normals = mesh.normals;
        }
        
        // Create buffers
        ComputeBuffer vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        ComputeBuffer normalBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        ComputeBuffer shadingBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 4);
        
        // Set data
        vertexBuffer.SetData(modifiedVertices);
        normalBuffer.SetData(normals);
        
        // Set noise parameters using SimpleNoiseSettings
        PRNG prng = new PRNG(seed);

        // Use SimpleNoiseSettings to set parameters on compute shader
        if (biomeNoise != null)
        {
            biomeNoise.SetComputeValues(shadingComputeShader, prng, "_biome");
        }
        else
        {
            Debug.LogWarning("Biome noise settings not assigned! Using defaults.");
            // You could create a default SimpleNoiseSettings here if needed
        }

        if (detailNoise != null)
        {
            detailNoise.SetComputeValues(shadingComputeShader, prng, "_detail");
        }
        else
        {
            Debug.LogWarning("Detail noise settings not assigned! Using defaults.");
            // You could create a default SimpleNoiseSettings here if needed
        }
        
        // Set buffers and parameters
        int kernelIndex = 0;
        shadingComputeShader.SetBuffer(kernelIndex, "vertices", vertexBuffer);
        shadingComputeShader.SetBuffer(kernelIndex, "normals", normalBuffer);
        shadingComputeShader.SetBuffer(kernelIndex, "shadingData", shadingBuffer);
        shadingComputeShader.SetInt("numVertices", vertexCount);
        
        // Dispatch
        int threadGroupSize = 64;
        int numGroups = Mathf.CeilToInt(vertexCount / (float)threadGroupSize);
        shadingComputeShader.Dispatch(kernelIndex, numGroups, 1, 1);
        
        // Get results
        shadingData = new Vector4[vertexCount];
        shadingBuffer.GetData(shadingData);
        
        // Store in mesh UVs (Vector4 can be stored in UVs)
        mesh.SetUVs(0, shadingData);
        
        // Cleanup
        vertexBuffer.Release();
        normalBuffer.Release();
        shadingBuffer.Release();
        
        Debug.Log($"Generated shading data for {vertexCount} vertices");
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

    public void GenerateCollisionMesh()
    {
        if (collisionMesh == null)
        {
            collisionMesh = new Mesh();
        }
        collisionMesh.name = "Collision Mesh";
        
        // [FIX] Actually use the collisionResolution to generate a simpler mesh
        // This stops the "Physics.PhysX cleaning mesh failed" error
        GenerateMeshAtResolution(collisionResolution, ref collisionMesh);
        
        // Add or update MeshCollider
        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<MeshCollider>();
        }
        collider.sharedMesh = collisionMesh;
        collider.convex = false; 
        
        Debug.Log($"Generated collision mesh with {collisionMesh.vertexCount} vertices");
    }
    
    void OnDestroy()
    {
        ReleaseBuffers();
    }
    
    [ContextMenu("Regenerate Planet")]
    void Regenerate()
    {
        GeneratePlanet();
    }
    
    void OnValidate()
    {
        // Only regenerate if we have all required components
        if (heightCompute == null || GetComponent<MeshFilter>() == null)
        {
            return;
        }
        
        // Check if we can run compute shaders
        #if UNITY_EDITOR
        // Don't run during compilation
        if (UnityEditor.EditorApplication.isCompiling)
        {
            return;
        }
        #endif
        
        // Regenerate the planet when parameters change
        // Use a small delay to avoid multiple calls during rapid changes
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // In edit mode, use delayCall to ensure it runs after all validations
            UnityEditor.EditorApplication.delayCall -= RegeneratePlanetDelayed;
            UnityEditor.EditorApplication.delayCall += RegeneratePlanetDelayed;
        }
        else
        #endif
        {
            GeneratePlanet();
        }
    }
    
    #if UNITY_EDITOR
    void RegeneratePlanetDelayed()
    {
        UnityEditor.EditorApplication.delayCall -= RegeneratePlanetDelayed;
        if (this != null && enabled && heightCompute != null)
        {
            GeneratePlanet();
            UnityEditor.SceneView.RepaintAll();
        }
    }
    #endif


    [ContextMenu("Create Default Noise Settings")]
    public void CreateDefaultNoiseSettings()
    {
        // Create biome noise settings
        if (biomeNoise == null)
        {
            biomeNoise = new SimpleNoiseSettings();
            biomeNoise.scale = 0.5f;
            biomeNoise.numLayers = 4;
            biomeNoise.persistence = 0.5f;
            biomeNoise.lacunarity = 2f;
        }
        
        // Create detail noise settings
        if (detailNoise == null)
        {
            detailNoise = new SimpleNoiseSettings();
            detailNoise.scale = 2f;
            detailNoise.numLayers = 6;
            detailNoise.persistence = 0.5f;
            detailNoise.lacunarity = 2f;
        }
        
        Debug.Log("Created default noise settings. You can now randomize them!");
    }

    [ContextMenu("Create Default Terrain Settings")]
    public void CreateDefaultTerrainSettings()
    {
        // Create continent noise
        if (continentNoise == null)
        {
            continentNoise = new SimpleNoiseSettings();
            continentNoise.scale = 0.3f; // Large features
            continentNoise.numLayers = 4;
            continentNoise.persistence = 0.5f;
            continentNoise.lacunarity = 2f;
        }
        
        // Create continent mask
        if (continentMaskNoise == null)
        {
            continentMaskNoise = new SimpleNoiseSettings();
            continentMaskNoise.scale = 0.5f;
            continentMaskNoise.numLayers = 3;
        }
        
        // Create mountain noise
        if (mountainNoise == null)
        {
            mountainNoise = new RidgeNoiseSettings();
            mountainNoise.scale = 1.5f;
            mountainNoise.numLayers = 5;
            mountainNoise.power = 2f;
            mountainNoise.gain = 1f;
        }
        
        Debug.Log("Created default terrain settings!");
    }
}
