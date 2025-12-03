using UnityEngine;

[CreateAssetMenu(fileName = "ComputeShape", menuName = "Scriptable Objects/ComputeShape")]
public abstract class ComputeShape : ScriptableObject
{
    public int seed;
    public ComputeShader heightCompute;
    
    public abstract float[] CalculateHeights(ComputeBuffer vertexBuffer);
    public abstract void ReleaseBuffers();
}
