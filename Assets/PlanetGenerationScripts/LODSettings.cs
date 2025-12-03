using UnityEngine;

[System.Serializable]
public class LODSettings
{
    public int lod0 = 200;
    public int lod1 = 100;
    public int lod2 = 50;
    
    public int GetLODResolution(int lodLevel)
    {
        switch (lodLevel)
        {
            case 0: return lod0;
            case 1: return lod1;
            case 2: return lod2;
            default: return lod2;
        }
    }
}
