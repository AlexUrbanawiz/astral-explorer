using UnityEngine;


[System.Serializable]
public class NoiseSettings
{
    public enum FilterType { Simple, Ridgid }
    public FilterType filterType;
    [ConditionalHide("filterType", 0)]
    public SimpleNoiseSettings simpleNoiseSettings;
    [ConditionalHide("filterType", 1)]
    public RidgidNoiseSettings ridgidNoiseSettings;



    [System.Serializable]
    public class SimpleNoiseSettings
    {
        public float strength = 1;
        [Range(1, 8)]
        public int numLayers = 1;
        public float baseRoughness = 1;
        public float roughness = 2;
        public float persistence = .5f;

        public bool randomCenter;
        public Vector3 center;

        public Vector3 RandomizeCenter()
        {
            return new Vector3(Random.Range(-1000, 1000), Random.Range(-1000, 1000), Random.Range(-1000, 1000));
        }

        public float minValue;
    }

    [System.Serializable]
    public class RidgidNoiseSettings : SimpleNoiseSettings
    {
        public float weightMultiplier = .8f;
    }

    
}
