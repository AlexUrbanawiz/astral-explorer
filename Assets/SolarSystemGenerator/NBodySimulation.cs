using UnityEngine;

public class NBodySimulation : MonoBehaviour
{
    CelestialBody[] bodies;
    static NBodySimulation instance;
    void Awake()
    {
        bodies = FindObjectsByType<CelestialBody>(FindObjectsSortMode.None);
        Time.fixedDeltaTime = Universe.physicsTimeStep;
    }

    void FixedUpdate()
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            Vector3 acceleration = CalculateAcceleration (bodies[i].position, bodies[i]);
            bodies[i].UpdateVelocity (acceleration, Universe.physicsTimeStep);
        }
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].UpdatePosition(Universe.physicsTimeStep);
        }
    }

    public static Vector3 CalculateAcceleration(Vector3 point, CelestialBody ignoreBody = null)
    {
        Vector3 acceleration = Vector3.zero;
        foreach (var body in Instance.bodies)
        {
            if (body != ignoreBody)
            {
                float sqrDst = (body.position - point).sqrMagnitude;
                Vector3 forceDir = (body.position - point).normalized;
                acceleration += forceDir * Universe.gravitationalConstant * body.mass / sqrDst;
            }
        }

        return acceleration;
    }
    
    public static CelestialBody[] Bodies {
        get {
            return Instance.bodies;
        }
    }


    static NBodySimulation Instance {
        get {
            if (instance == null) {
                instance = FindFirstObjectByType<NBodySimulation>();
            }
            return instance;
        }
    }
    
}
