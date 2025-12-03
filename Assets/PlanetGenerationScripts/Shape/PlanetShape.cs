using UnityEngine;

[CreateAssetMenu(fileName = "PlanetShape", menuName = "Scriptable Objects/PlanetShape")]
public class PlanetShape : ScriptableObject
{
    public bool randomize;
    public int seed;
    public ComputeShader heightMapCompute;
    
    public bool perturbVertices;
    public ComputeShader perturbCompute;
    [Range(0, 1)]
    public float perturbStrength = 0.7f;
    
    public event System.Action OnSettingChanged;
    
    ComputeBuffer heightBuffer;
    
    public virtual float[] CalculateHeights(ComputeBuffer vertexBuffer)
    {
        SetShapeData();
        heightMapCompute.SetInt("numVertices", vertexBuffer.count);
        heightMapCompute.SetBuffer(0, "vertices", vertexBuffer);
        ComputeHelper.CreateAndSetBuffer<float>(ref heightBuffer, vertexBuffer.count, heightMapCompute, "heights");
        
        ComputeHelper.Run(heightMapCompute, vertexBuffer.count);
        
        var heights = new float[vertexBuffer.count];
        heightBuffer.GetData(heights);
        return heights;
    }
    
    public virtual void ReleaseBuffers()
    {
        ComputeHelper.Release(heightBuffer);
    }
    
    protected virtual void SetShapeData()
    {
        // Override in derived classes
    }
    
    protected virtual void OnValidate()
    {
        if (OnSettingChanged != null)
        {
            OnSettingChanged();
        }
    }
}