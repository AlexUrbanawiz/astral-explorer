using UnityEngine;

[CreateAssetMenu(menuName = "Planet/Settings")]
public class PlanetSettings : ScriptableObject
{
    public PlanetShape shape;
    public PlanetShading shading;
}