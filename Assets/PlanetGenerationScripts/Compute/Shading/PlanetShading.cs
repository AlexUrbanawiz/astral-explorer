using UnityEngine;

public abstract class PlanetShading : ScriptableObject
{
    public event System.Action OnSettingChanged;
    
    public bool randomize;
    public int seed;
    
    public Material terrainMaterial;
    public bool hasOcean;
    [Range(0, 1)]
    public float oceanLevel;
    
    public ComputeShader shadingDataCompute;
    
    protected Vector4[] cachedShadingData;
    ComputeBuffer shadingBuffer;
    
    public virtual void Initialize(PlanetShape shape) { }
    
    public Vector4[] GenerateShadingData(ComputeBuffer vertexBuffer)
    {
        int numVertices = vertexBuffer.count;
        Vector4[] shadingData = new Vector4[numVertices];
        
        if (shadingDataCompute)
        {
            SetShadingDataComputeProperties();
            
            shadingDataCompute.SetInt("numVertices", numVertices);
            shadingDataCompute.SetBuffer(0, "vertices", vertexBuffer);
            ComputeHelper.CreateAndSetBuffer<Vector4>(ref shadingBuffer, numVertices, shadingDataCompute, "shadingData");
            
            ComputeHelper.Run(shadingDataCompute, numVertices);
            
            shadingBuffer.GetData(shadingData);
        }
        
        cachedShadingData = shadingData;
        return shadingData;
    }
    
    public virtual void SetTerrainProperties(Material material, Vector2 heightMinMax, float bodyScale)
    {
        // Override in derived classes
    }
    
    protected virtual void SetShadingDataComputeProperties()
    {
        // Override in derived classes
    }
    
    public virtual void ReleaseBuffers()
    {
        ComputeHelper.Release(shadingBuffer);
    }
    
    protected virtual void OnValidate()
    {
        if (OnSettingChanged != null)
        {
            OnSettingChanged();
        }
    }
}
