using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RandomOrbitGenerator))]
public class RandomOrbitGeneratorEditor : Editor
{
    RandomOrbitGenerator randomOrbit;

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Generate Star"))
        {
            randomOrbit.GenerateStar();
        }
        if (GUILayout.Button("Generate Lone Planet"))
        {
            randomOrbit.GenerateLonePlanet();
        }
        if (GUILayout.Button("Generate Planet w/ Moon"))
        {
            randomOrbit.GeneratePlanetWithMoon();
        }
        if (GUILayout.Button("Generate Planet w/ Moons"))
        {
            randomOrbit.GeneratePlanetWithMoons(randomOrbit.numMoons);
        }
        if (GUILayout.Button("Generate Binary Planets"))
        {
            randomOrbit.GenerateBinaryPlanets();
        }
        if (GUILayout.Button("Generate System"))
        {
            randomOrbit.GenerateSystem();
        }
        if (GUILayout.Button("Check Hill Radius"))
        {
            Debug.Log(randomOrbit.CalculateHillRadius("test"));
        }
        base.OnInspectorGUI();
        
    }


    private void OnEnable()
    {
        randomOrbit = (RandomOrbitGenerator)target;
    }
}
