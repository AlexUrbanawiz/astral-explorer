using UnityEngine;
using UnityEngine.UIElements;

public class SimpleComputeHeightGenerator : MonoBehaviour
{
    public ComputeShader heightComputeShader;
    public int testVertexCount = 1000;

    ComputeBuffer vertexBuffer;
    ComputeBuffer heightBuffer;

    void Start()
    {
        TestComputeShader();
    }

    void TestComputeShader()
    {
        if(heightComputeShader == null)
        {
            Debug.Log("No shader assigned");
            return;
        }

        Vector3[] vertices = new Vector3[testVertexCount];
        for (int i = 0; i < testVertexCount; i++)
        {
            vertices[i] = Random.onUnitSphere;
        }
        vertexBuffer = new ComputeBuffer(testVertexCount, sizeof(float) * 3);
        heightBuffer = new ComputeBuffer(testVertexCount, sizeof(float));

        vertexBuffer.SetData(vertices);

        int kernalIndex = 0;
        heightComputeShader.SetBuffer(kernalIndex, "vertices", vertexBuffer);
        heightComputeShader.SetBuffer(kernalIndex, "heights", heightBuffer);
        heightComputeShader.SetInt("numVertices", testVertexCount);
        heightComputeShader.SetFloat("heightMultiplier", 0.1f);
        
        int threadGroupSize = 64;
        int numGroups = Mathf.CeilToInt(testVertexCount / (float)threadGroupSize);
        heightComputeShader.Dispatch(kernalIndex, numGroups, 1, 1);

        float[] heights = new float[testVertexCount];
        heightBuffer.GetData(heights);

        Debug.Log($"Computed {heights.Length} heights");
        Debug.Log($"Min: {System.Linq.Enumerable.Min(heights)}, Max: {System.Linq.Enumerable.Max(heights)}");

        ReleaseBuffers();
    }

    void ReleaseBuffers()
    {
        if (vertexBuffer != null) vertexBuffer.Release();
        if (heightBuffer != null) heightBuffer.Release();
    }
    void OnDestroy()
    {
        ReleaseBuffers();
    }
}




