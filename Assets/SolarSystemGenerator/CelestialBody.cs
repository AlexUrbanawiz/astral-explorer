using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
public class CelestialBody : GravityObject
{
    public float radius;
    public float surfaceGravity;
    public Vector3 initialVelocity;
    public Vector3 currentVelocity { get; private set; }
    // public Vector3 position;
    public Quaternion orbitalPlaneRotation = Quaternion.identity;
    public float mass { get; private set; }
    Rigidbody rb;
    public Transform meshHolder;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        transform.localScale = new Vector3(radius, radius, radius);
        // transform.position = position;
        currentVelocity = initialVelocity;
    }

    public void UpdateVelocity(CelestialBody[] allBodies, float timeStep)
    {
        foreach (var otherBody in allBodies)
        {
            if (otherBody != this)
            {
                float sqrDst = (otherBody.position - rb.position).sqrMagnitude;
                Vector3 forceDir = (otherBody.position - rb.position).normalized;
                Vector3 acceleration = forceDir * Universe.gravitationalConstant * otherBody.mass / sqrDst;
                currentVelocity += acceleration * timeStep;
            }
        }
    }

    public void UpdateVelocity (Vector3 acceleration, float timeStep) {
        currentVelocity += acceleration * timeStep;
    }

    void OnValidate()
    {
        mass = surfaceGravity * radius * radius / Universe.gravitationalConstant;
        transform.localScale = Vector3.one * radius;
        // transform.position = position;
        rb.mass = mass;
        meshHolder = transform.GetChild (0);
    }

    public void UpdateValues()
    {
        mass = surfaceGravity * radius * radius / Universe.gravitationalConstant;
        transform.localScale = new Vector3(radius, radius, radius);
        transform.position = position;
        rb.mass = mass;
        meshHolder = transform.GetChild (0);
    }

    public void UpdatePosition(float timeStep)
    {
        rb.MovePosition(rb.position + currentVelocity * timeStep);
    }
    public void SetPosition(Vector3 position)
    {
        rb.position = position;
    }

    
    public Vector3 position
    {
        get
        {
            return rb.position;
        }
    }
    
}
