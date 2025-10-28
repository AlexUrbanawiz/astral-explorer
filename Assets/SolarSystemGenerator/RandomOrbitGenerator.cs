using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class RandomOrbitGenerator : MonoBehaviour
{
    [Header("Full random system ranges")]
    public Vector2 numPlanetsRange = new Vector2(2, 6);
    public Vector2 numMoonRange = new Vector2(2, 4);
    public SystemGenerationOption[] generationOptions;

    [Header("Ranges")]
    public Vector2 mainStarRadius = new Vector2(15000, 20000);
    public Vector2 mainStarSG = new Vector2(30, 60);
    public Vector2 planetRadius = new Vector2(200, 600);
    public Vector2 planetSG = new Vector2(7, 16);
    public Vector2 biPlanetRadius = new Vector2(300, 400);
    public Vector2 biPlanetSG = new Vector2(9, 11);
    public Vector2 moonRadius = new Vector2(20, 40);
    public Vector2 moonSG = new Vector2(1, 5);
    public Vector2 distancePlanetStar = new Vector2(10000, 13000);
    public Vector2 distancePlanetPlanet = new Vector2(19000, 32000);
    public Vector2 distancePlanetMoon = new Vector2(400, 1200);
    public Vector2 distancePlanetMutliMoon = new Vector2(400, 600);
    public Vector2 distanceMoonMoon = new Vector2(0, 0);
    public Vector2 distanceBinaryPlanets = new Vector2(900, 1300);
    public float hillRadiusTolerance = 400;
    public Vector2 distanceBiPlanetStar = new Vector2(13000, 16000);
    public int numMoons;

    [Header("Prefabs")]
    public GameObject baseStar;
    public GameObject basePlanet;
    public GameObject baseMoon;

    [Header("Materials")]
    public Material[] materials;

    [Header("Objects")]
    CelestialBody mainStar;
    CelestialBody mostRecentPlanet;
    CelestialBody mostRecentMoon;
    CelestialBody mostRecentBiPlanetA;
    CelestialBody mostRecentBiPlanetB;
    public Transform bodySim;




    public void GenerateSystem()
    {
        GenerateStar();
        int numPlanets = Random.Range((int)numPlanetsRange.x, (int)numPlanetsRange.y);
        for (int i = 0; i < numPlanets; i++)
        {
            string randomResult = GetWeightedRandomSystem();
            switch (randomResult)
            {
                case "Lone Planet":
                    GenerateLonePlanet();
                    break;
                case "Planet with Moon":
                    GeneratePlanetWithMoon();
                    break;
                case "Planet with Moons":
                    int numMoons = Random.Range((int)numMoonRange.x, (int)numMoonRange.y);
                    GeneratePlanetWithMoons(numMoons);
                    break;
                case "Binary Planets":
                    GenerateBinaryPlanets();
                    break;
            }
        }
    }



    public void GenerateStar()
    {
        GameObject star = Instantiate(baseStar, Vector3.zero, transform.rotation, bodySim);
        star.name = "Sun";
        int radius = Random.Range((int)mainStarRadius.x, (int)mainStarRadius.y);
        float surfaceGravity = Random.Range(mainStarSG.x, mainStarSG.y);
        CelestialBody celestialBody = star.GetComponent<CelestialBody>();
        celestialBody.radius = radius;
        celestialBody.surfaceGravity = surfaceGravity;
        celestialBody.SetPosition(Vector3.zero);
        celestialBody.UpdateValues();
        GetComponent<OrbitDebugDisplay>().centralBody = celestialBody;
        mainStar = celestialBody;

    }

    public void GenerateLonePlanet()
    {
        GameObject planet = Instantiate(basePlanet, Vector3.zero, transform.rotation, bodySim);
        CelestialBody celestialBody = planet.GetComponent<CelestialBody>();
        Quaternion orbitalRotation = GetRandomOrbitalRotation();
        celestialBody.orbitalPlaneRotation = orbitalRotation;
        Vector3 planetPosition;
        float distance;
        if (mostRecentPlanet == null)
        {
            distance = Mathf.Round(Random.Range(distancePlanetStar.x, distancePlanetStar.y));
        }
        else
        {
            float lastOrbitalRadius = Vector3.Distance(mainStar.position, mostRecentPlanet.position);
            float sepDistance = Mathf.Round(Random.Range(distancePlanetPlanet.x, distancePlanetPlanet.y));
            distance = sepDistance + lastOrbitalRadius;
        }
        Vector3 initialRadialVector = new Vector3(distance, 0f, 0f);
        planetPosition = mainStar.position + (orbitalRotation * initialRadialVector);

        int radius = Random.Range((int)planetRadius.x, (int)planetRadius.y);
        float surfaceGravity = Random.Range(planetSG.x, planetSG.y);
        celestialBody.radius = radius;
        celestialBody.surfaceGravity = surfaceGravity;
        celestialBody.SetPosition(planetPosition);
        celestialBody.UpdateValues();
        celestialBody.initialVelocity = CalculateInitalVelocity(celestialBody);
        Material randomMaterial = materials[Random.Range(0, materials.Length - 1)];
        celestialBody.meshHolder.GetComponent<TerrainGenerator>().material = randomMaterial;
        planet.name = $"{randomMaterial.name} Planet";
        mostRecentPlanet = celestialBody;
    }
    public void GenerateLonePlanet(string extraDense)
    {
        GameObject planet = Instantiate(basePlanet, Vector3.zero, transform.rotation, bodySim);
        CelestialBody celestialBody = planet.GetComponent<CelestialBody>();
        Quaternion orbitalRotation = GetRandomOrbitalRotation();
        celestialBody.orbitalPlaneRotation = orbitalRotation;
        Vector3 planetPosition;
        float distance;
        if (mostRecentPlanet == null)
        {
            distance = Mathf.Round(Random.Range(distancePlanetStar.x, distancePlanetStar.y));
        }
        else
        {
            float lastOrbitalRadius = Vector3.Distance(mainStar.position, mostRecentPlanet.position);
            float sepDistance = Mathf.Round(Random.Range(distancePlanetPlanet.x, distancePlanetPlanet.y));
            distance = sepDistance + lastOrbitalRadius;
        }
        Vector3 initialRadialVector = new Vector3(distance, 0f, 0f);
        planetPosition = mainStar.position + (orbitalRotation * initialRadialVector);

        int radius = Random.Range((int)planetRadius.x, (int)planetRadius.y);
        float surfaceGravity = 2*Random.Range(planetSG.x, planetSG.y);
        celestialBody.radius = radius;
        celestialBody.surfaceGravity = surfaceGravity;
        celestialBody.SetPosition(planetPosition);
        celestialBody.UpdateValues();
        celestialBody.initialVelocity = CalculateInitalVelocity(celestialBody);
        Material randomMaterial = materials[Random.Range(0, materials.Length - 1)];
        celestialBody.meshHolder.GetComponent<TerrainGenerator>().material = randomMaterial;
        planet.name = $"{randomMaterial.name} Planet";
        mostRecentPlanet = celestialBody;
    }

    public void GenerateMoon(Vector2 dstRange)
    {
        GameObject moon = Instantiate(baseMoon, Vector3.zero, transform.rotation, bodySim);
        CelestialBody celestialBody = moon.GetComponent<CelestialBody>();
        int radius = Random.Range((int)moonRadius.x, (int)moonRadius.y);
        int surfaceGravity = Random.Range((int)moonSG.x, (int)moonSG.y);    
        float hillRadius = CalculateHillRadius();
        float distance;
        do
            distance = Random.Range(dstRange.x, dstRange.y);
        while (distance >= hillRadius - hillRadiusTolerance);
        Quaternion planetRotation = mostRecentPlanet.orbitalPlaneRotation;
        Quaternion ringPlacementRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        Quaternion fullMoonRotation = planetRotation * ringPlacementRotation;
        Vector3 initialRadialVector = new Vector3(distance, 0, 0);
        Vector3 position = mostRecentPlanet.position + (fullMoonRotation * initialRadialVector);
        celestialBody.radius = radius;
        celestialBody.surfaceGravity = surfaceGravity;
        celestialBody.SetPosition(position);
        celestialBody.UpdateValues();
        Vector3 moonInitV = CalculateInitalVelocity(mostRecentPlanet, celestialBody);
        Vector3 initV = moonInitV + mostRecentPlanet.initialVelocity;
        celestialBody.initialVelocity = initV;
        moon.name = $"{mostRecentPlanet.name} Moon";
        mostRecentMoon = celestialBody;
    }

    public void GenerateMoon(float recentMoonDst)
    {
        GameObject moon = Instantiate(baseMoon, Vector3.zero, transform.rotation, bodySim);
        CelestialBody celestialBody = moon.GetComponent<CelestialBody>();
        int radius = Random.Range((int)moonRadius.x, (int)moonRadius.y);
        int surfaceGravity = Random.Range((int)moonSG.x, (int)moonSG.y);
        float hillRadius = CalculateHillRadius();
        float distance;
        int count = 0;
        bool failedSpawn = false;
        distance = (recentMoonDst * 1.5f) + Random.Range(distanceMoonMoon.x, distanceMoonMoon.y);
        while (distance >= hillRadius - hillRadiusTolerance)
        {
            if (count >= 25)
            {
                Debug.Log($"Failed spawn with distance: {distance}");
                distance = 0;
                failedSpawn = true;
                break;
            }
            distance = (recentMoonDst * 1.5f) + Random.Range(distanceMoonMoon.x, distanceMoonMoon.y);
            count++;
        }
        Quaternion planetRotation = mostRecentPlanet.orbitalPlaneRotation;
        Quaternion ringPlacementRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        Quaternion fullMoonRotation = planetRotation * ringPlacementRotation;
        Vector3 initialRadialVector = new Vector3(distance, 0, 0);
        Vector3 position = mostRecentPlanet.position + (fullMoonRotation * initialRadialVector);
        celestialBody.radius = radius;
        celestialBody.surfaceGravity = surfaceGravity;
        celestialBody.SetPosition(position);
        celestialBody.UpdateValues();
        Vector3 moonInitV = CalculateInitalVelocity(mostRecentPlanet, celestialBody);
        Vector3 initV = moonInitV + mostRecentPlanet.initialVelocity;
        celestialBody.initialVelocity = initV;
        if (!failedSpawn)
        {
            mostRecentMoon = celestialBody;
        }
        else
        {
            moon.SetActive(false);
        }
    }

    public void GeneratePlanetWithMoons(int numMoons)
    {
        GenerateLonePlanet("Extra Dense");
        GenerateMoon(distancePlanetMutliMoon);
        mostRecentMoon.name = $"{mostRecentMoon.name} 1";
        for (int i = 1; i < numMoons; i++)
        {
            float recentMoonDst = (mostRecentMoon.position - mostRecentPlanet.position).magnitude;
            CelestialBody pastMoon = mostRecentMoon;
            GenerateMoon(recentMoonDst);
            mostRecentMoon.name = $"{pastMoon.name.Substring(0, (pastMoon.name.Count() - 1))}{i + 1}";
        }
    }
    public void GeneratePlanetWithMoon()
    {
        GenerateLonePlanet();
        GenerateMoon(distancePlanetMoon);
    }


    public void GenerateBinaryPlanets()
    {
        Quaternion orbitalRotation = GetRandomOrbitalRotation();
        Vector3 barycenterPosition;
        float distance;
        if (mostRecentPlanet == null)
        {
            distance = Mathf.Round(Random.Range(distanceBiPlanetStar.x, distanceBiPlanetStar.y));
        }
        else
        {
            float lastOrbitalRadius = Vector3.Distance(mainStar.position, mostRecentPlanet.position);
            float sepDistance = Mathf.Round(Random.Range(distancePlanetPlanet.x, distancePlanetPlanet.y));
            distance = sepDistance + lastOrbitalRadius;
        }
        float sepDst = Mathf.Round(Random.Range(distanceBinaryPlanets.x, distanceBinaryPlanets.y));
        mostRecentBiPlanetA = GeneratePlanet();
        mostRecentBiPlanetB = GeneratePlanet();
        float dstA = sepDst * (mostRecentBiPlanetB.mass / (mostRecentBiPlanetA.mass + mostRecentBiPlanetB.mass));
        float dstB = sepDst * (mostRecentBiPlanetA.mass / (mostRecentBiPlanetA.mass + mostRecentBiPlanetB.mass));
        //Adding dstA to the inital Radial vector to make sure that both planets generate within the distancePlanetStar range.
        Vector3 initialRadialVector = new Vector3(distance, 0f, 0f);
        barycenterPosition = mainStar.position + (orbitalRotation * initialRadialVector);
        mostRecentBiPlanetA.orbitalPlaneRotation = orbitalRotation;
        mostRecentBiPlanetB.orbitalPlaneRotation = orbitalRotation;
        Vector3 separationAxis = orbitalRotation * Vector3.right;
        mostRecentBiPlanetA.SetPosition(barycenterPosition - (separationAxis * dstA));
        mostRecentBiPlanetB.SetPosition(barycenterPosition + (separationAxis * dstB));
        Vector3[] initVs = CalculateInitalVelocity(barycenterPosition, mostRecentBiPlanetA, mostRecentBiPlanetB, sepDst, orbitalRotation);
        mostRecentBiPlanetA.initialVelocity = initVs[0];
        mostRecentBiPlanetB.initialVelocity = initVs[1];
        mostRecentBiPlanetA.UpdateValues();
        mostRecentBiPlanetB.UpdateValues();
        mostRecentPlanet = mostRecentBiPlanetB;


    }
    public CelestialBody GeneratePlanet()
    {
        GameObject planet = Instantiate(basePlanet, Vector3.zero, transform.rotation, bodySim);
        CelestialBody celestialBody = planet.GetComponent<CelestialBody>();
        int radius = Random.Range((int)biPlanetRadius.x, (int)biPlanetRadius.y);
        float surfaceGravity = Random.Range(biPlanetSG.x, biPlanetSG.y);
        celestialBody.radius = radius;
        celestialBody.surfaceGravity = surfaceGravity;
        celestialBody.UpdateValues();
        Material randomMaterial = materials[Random.Range(0, materials.Length - 1)];
        celestialBody.meshHolder.GetComponent<TerrainGenerator>().material = randomMaterial;
        planet.name = $"{randomMaterial.name} Planet";
        return celestialBody;
    }

    //Calculating initial velocity for planets
    Vector3 CalculateInitalVelocity(CelestialBody planet)
    {
        float dst = Vector3.Distance(mainStar.position, planet.position);
        float vMag = Mathf.Sqrt((Universe.gravitationalConstant * mainStar.mass) / dst);
        Vector3 vDir = planet.orbitalPlaneRotation * Vector3.forward;
        Vector3 initV = vMag * vDir;
        return initV;
    }
    //Calculating initoal velocity for moons orbiting a planet
    Vector3 CalculateInitalVelocity(CelestialBody planet, CelestialBody moon)
    {
        float dst = Vector3.Distance(planet.position, moon.position);
        float vMag = Mathf.Sqrt((Universe.gravitationalConstant * planet.mass) / dst);
        Vector3 orbitalNormal = planet.orbitalPlaneRotation * Vector3.up;
        Vector3 radialVector = moon.position - planet.position;
        Vector3 vDir = Vector3.Cross(orbitalNormal, radialVector.normalized);
        Vector3 initV = vMag * vDir;
        return initV;
    }
    //Calculating initial velocity for binary planets
    Vector3[] CalculateInitalVelocity(Vector3 barycenterPosition, CelestialBody planetA, CelestialBody planetB, float sepDst, Quaternion orbitalRotation)
    {
        //Barycenter velocity calculations
        float baryDst = Vector3.Distance(mainStar.position, barycenterPosition);
        float baryVMag = Mathf.Sqrt((Universe.gravitationalConstant * mainStar.mass) / baryDst);
        Vector3 baryVDir = orbitalRotation * Vector3.forward;
        Vector3 baryInitV = baryVMag * baryVDir;

        Vector3 internalOrbitalNormal = orbitalRotation * Vector3.up;

        //Planet A calculations
        float aVMag = Mathf.Sqrt((Universe.gravitationalConstant * (planetB.mass * planetB.mass)) / (sepDst * (planetA.mass + planetB.mass)));
        Vector3 radialVectorA = planetA.position - barycenterPosition;
        Vector3 aVDir = Vector3.Cross(internalOrbitalNormal, radialVectorA.normalized);
        Vector3 aOrbitV = aVMag * aVDir;

        //Planet B calculations
        float bVMag = Mathf.Sqrt((Universe.gravitationalConstant * (planetA.mass * planetA.mass)) / (sepDst * (planetA.mass + planetB.mass)));
        Vector3 radialVectorB = planetB.position - barycenterPosition;
        Vector3 bVDir = -Vector3.Cross(radialVectorB.normalized, internalOrbitalNormal);
        Vector3 bOrbitV = bVMag * bVDir;

        Vector3 aInitV = baryInitV + aOrbitV;
        Vector3 bInitV = baryInitV + bOrbitV;
        Vector3[] initVs = { aInitV, bInitV };
        return initVs;
    }

    public float CalculateHillRadius()
    {
        float a = Vector3.Distance(mainStar.position, mostRecentPlanet.position);
        float radius = a * Mathf.Pow(mostRecentPlanet.mass / (3 * mainStar.mass), 1f / 3f);
        return radius;
    }
    public string CalculateHillRadius(string test)
    {
        float a = mostRecentPlanet.position.x;
        float radius = a * Mathf.Pow(mostRecentPlanet.mass / (3 * mainStar.mass), 1f / 3f);
        float moonDst = (mostRecentMoon.position - mostRecentPlanet.position).magnitude;
        return $"radius: {radius}, a: {a}, planet mass: {mostRecentPlanet.mass}, star mass: {mainStar.mass}, moonDst: {moonDst}";

    }

    Quaternion GetRandomOrbitalRotation()
    {
        float inclination = Random.Range(0f, 15f);
        float rotationAboutY = Random.Range(0f, 360f);

        Quaternion rotationY = Quaternion.Euler(0f, rotationAboutY, 0f);
        Quaternion rotationX = Quaternion.Euler(inclination, 0f, 0f);
        return rotationY * rotationX;
    }



    string GetWeightedRandomSystem()
    {
    int totalWeight = 0;
    foreach (var option in generationOptions)
    {
        totalWeight += option._weight;
    }
    int randomNumber = Random.Range(0, totalWeight);
    int runningWeight = 0;
    foreach (var option in generationOptions)
    {
        runningWeight += option._weight;
        if (randomNumber < runningWeight)
        {
            return option._name;
        }
    }
    return ""; 
}
}

[System.Serializable]
public struct SystemGenerationOption
{
    public string _name;
    public int _weight;
}