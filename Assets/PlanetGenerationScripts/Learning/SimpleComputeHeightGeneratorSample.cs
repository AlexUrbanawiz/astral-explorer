using UnityEngine;
using System.Linq;

/// <summary>
/// Lesson 3.2: Simple example of calling a compute shader from C#.
/// This demonstrates the CPU-GPU communication pipeline.
/// </summary>
public class SimpleComputeHeightGeneratorSample : MonoBehaviour
{
    [Header("Compute Shader")]
    [Tooltip("Assign a compute shader here. Create one following Lesson 3.3.")]
    public ComputeShader heightComputeShader;
    
    [Header("Test Settings")]
    [Tooltip("Number of test vertices to generate.")]
    public int testVertexCount = 1000;
    
    [Tooltip("Height multiplier for the compute shader.")]
    public float heightMultiplier = 0.1f;
    
    ComputeBuffer vertexBuffer;
    ComputeBuffer heightBuffer;
    
    void Start()
    {
        if (heightComputeShader != null)
        {
            TestComputeShader();
        }
        else
        {
            Debug.LogWarning("No compute shader assigned! Create one following Lesson 3.3.");
        }
    }
    
    /// <summary>
    /// Demonstrates the complete pipeline for calling a compute shader:
    /// 1. Create test data
    /// 2. Create GPU buffers
    /// 3. Set data on buffers
    /// 4. Set buffers and parameters on shader
    /// 5. Dispatch (run) the shader
    /// 6. Get results back
    /// 7. Clean up
    /// </summary>
    void TestComputeShader()
    {
        if (heightComputeShader == null)
        {
            Debug.LogError("No compute shader assigned!");
            return;
        }
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // STEP 1: Create test vertices (points on unit sphere)
        Vector3[] vertices = new Vector3[testVertexCount];
        for (int i = 0; i < testVertexCount; i++)
        {
            vertices[i] = Random.onUnitSphere;
        }
        
        // STEP 2: Create buffers
        // ComputeBuffer(count, stride) where stride is size in bytes
        vertexBuffer = new ComputeBuffer(testVertexCount, sizeof(float) * 3);
        heightBuffer = new ComputeBuffer(testVertexCount, sizeof(float));
        
        // STEP 3: Set data on buffers
        vertexBuffer.SetData(vertices);
        
        // STEP 4: Set buffers and parameters on shader
        int kernelIndex = 0; // First (and only) kernel in simple shader
        heightComputeShader.SetBuffer(kernelIndex, "vertices", vertexBuffer);
        heightComputeShader.SetBuffer(kernelIndex, "heights", heightBuffer);
        heightComputeShader.SetInt("numVertices", testVertexCount);
        heightComputeShader.SetFloat("heightMultiplier", heightMultiplier);
        
        // STEP 5: Dispatch (run) the shader
        // Calculate how many thread groups we need
        // The shader uses [numthreads(64, 1, 1)], so 64 threads per group
        int threadGroupSize = 64; // MUST match [numthreads] in compute shader!
        int numGroups = Mathf.CeilToInt(testVertexCount / (float)threadGroupSize);
        heightComputeShader.Dispatch(kernelIndex, numGroups, 1, 1);
        
        // STEP 6: Get results back from GPU
        float[] heights = new float[testVertexCount];
        heightBuffer.GetData(heights);
        
        sw.Stop();
        
        // STEP 7: Display results
        Debug.Log($"=== Compute Shader Test Results ===");
        Debug.Log($"Vertices processed: {heights.Length}");
        Debug.Log($"Min height: {heights.Min():F3}");
        Debug.Log($"Max height: {heights.Max():F3}");
        Debug.Log($"Average height: {heights.Average():F3}");
        Debug.Log($"Time: {sw.ElapsedMilliseconds}ms");
        
        // Clean up buffers
        ReleaseBuffers();
    }
    
    /// <summary>
    /// Always release compute buffers when done to free GPU memory.
    /// </summary>
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
    
    /// <summary>
    /// Clean up when component is destroyed.
    /// </summary>
    void OnDestroy()
    {
        ReleaseBuffers();
    }
    
    /// <summary>
    /// Test function that can be called from Inspector context menu.
    /// </summary>
    [ContextMenu("Run Compute Shader Test")]
    void RunTest()
    {
        TestComputeShader();
    }
}


