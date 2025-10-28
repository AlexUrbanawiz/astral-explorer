using UnityEngine;
using UnityEngine.InputSystem;

public class ShipController : GravityObject
{
    public LayerMask groundedMask;

    [Header ("Handling")]
    public float thrustStrength = 20;
    public float rotSpeed = 5;
    public float rollSpeed = 30;
    public float rotSmoothSpeed = 10;
    public bool lockCursor; 
    bool docked;
    [Header ("Landing")]
    public float maxLandingSpeed = 5f;
    public float alignmentForce = 100f;
    Rigidbody rb;
    Quaternion targetRot;
    Quaternion smoothedRot;
    
    public Vector3 thrusterInput;
    // PlayerController pilot;
    // bool shipIsPiloted;
    int numCollisionTouches;
    // bool hatchOpen;



    [Header("Input Actions")]
    public InputAction thrustInputActionX;
    public InputAction thrustInputActionY;
    public InputAction thrustInputActionZ;
    public InputAction yawInputAction;
    public InputAction pitchInputAction;
    public InputAction rollInputAction;
    public InputAction thrustInputAction;
    public InputAction pauseInputAction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    

    void Awake ()
    {
        pauseInputAction.Enable();
        thrustInputActionX.Enable();
        thrustInputActionY.Enable();
        thrustInputActionZ.Enable();
        yawInputAction.Enable();
        pitchInputAction.Enable();
        rollInputAction.Enable();

        InitRigidbody ();
        targetRot = transform.rotation;
        smoothedRot = transform.rotation;

        if (lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }


    void Update()
    {
        bool pauseInput = pauseInputAction.IsPressed();
        // if (pauseInput)
        // {
        //     ToggleDock();
        // }
        if (!docked)
        {
            HandleMovement();
        }
        
    }

    void HandleMovement()
    {
        
        thrusterInput = new Vector3(thrustInputActionX.ReadValue<float>(), thrustInputActionY.ReadValue<float>(), thrustInputActionZ.ReadValue<float>());
        float yawInput = 0;
        float pitchInput = 0;
        yawInput = yawInputAction.ReadValue<float>() * rotSpeed;
        pitchInput = pitchInputAction.ReadValue<float>() * rotSpeed;
        float rollInput = rollInputAction.ReadValue<float>() * rollSpeed * Time.deltaTime;


        if (numCollisionTouches == 0)
        {
            var yaw = Quaternion.AngleAxis(yawInput, transform.up);
            var pitch = Quaternion.AngleAxis(-pitchInput, transform.right);
            var roll = Quaternion.AngleAxis(-rollInput, transform.forward);

            targetRot = yaw * pitch * roll * targetRot;
            smoothedRot = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotSmoothSpeed);
        }
        else
        {
            targetRot = transform.rotation;
            smoothedRot = transform.rotation;
        }
    }

    void FixedUpdate()
    {
        if (!docked)
        {
            // Gravity
            Vector3 gravity = NBodySimulation.CalculateAcceleration(rb.position);
            rb.AddForce(gravity, ForceMode.Acceleration);

            // Thrusters
            Vector3 thrustDir = transform.TransformVector(thrusterInput);
            rb.AddForce(thrustDir * thrustStrength, ForceMode.Acceleration);

            // HandleRotationTorque();
        }
        // else
        // {
            // AlignToSurface();
        // }

        if (numCollisionTouches == 0) {
            rb.MoveRotation (smoothedRot);
        }
    }

    void HandleRotationTorque()
    {
        float pitchInput = pitchInputAction.ReadValue<float>();
        float yawInput = yawInputAction.ReadValue<float>();
        float rollInput = rollInputAction.ReadValue<float>();

        // Apply torque based on input
        rb.AddRelativeTorque(Vector3.right * -pitchInput * rotSpeed, ForceMode.Acceleration);
        rb.AddRelativeTorque(Vector3.up * yawInput * rotSpeed, ForceMode.Acceleration);
        rb.AddRelativeTorque(Vector3.forward * -rollInput * rollSpeed, ForceMode.Acceleration);

        // Simple Damping: Reduce angular velocity to prevent perpetual spin
        rb.angularVelocity *= 0.95f; // You can also adjust Angular Drag in the Rigidbody component
    }
    
    void AlignToSurface()
    {
        // 1. Calculate the 'up' direction (opposite of gravity)
        Vector3 gravity = NBodySimulation.CalculateAcceleration (rb.position);
        Vector3 surfaceUp = -gravity.normalized;
        
        // 2. Calculate the desired rotation (ship's 'up' to surface 'up')
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, surfaceUp) * transform.rotation;
        
        // 3. Apply the rotation. Since we are kinematic, we set rotation directly
        // This is faster and prevents the kinematic object from drifting.
        transform.rotation = targetRotation;
    }


    void InitRigidbody()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.centerOfMass = Vector3.zero;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }
    

    void OnCollisionEnter (Collision other) {
        if (groundedMask == (groundedMask | (1 << other.gameObject.layer))) {
            if (rb.linearVelocity.magnitude <= maxLandingSpeed)
            {
                ToggleDock();
            }
            numCollisionTouches++;
        }
        // float impactSpeed = other.relativeVelocity.magnitude;
        
        // if (impactSpeed <= maxLandingSpeed)
        // {
        //     // Soft landing: immediately stop and dock
        //     rb.linearVelocity = Vector3.zero;
        //     rb.angularVelocity = Vector3.zero;
        //     ToggleDock(true); // Dock the ship
        //     Debug.Log("Soft Landing");
        // } 
        // else 
        // {
        //     // Hard landing/crash: Still grounded, but perhaps apply damage or shake
        //     numCollisionTouches++;
        //     Debug.Log($"Crashed with speed of {impactSpeed}");
        // }
    }

    void OnCollisionExit(Collision other)
    {
        if (groundedMask == (groundedMask | (1 << other.gameObject.layer)))
        {
            numCollisionTouches--;
        }
    }

    void Land()
    {
        ToggleDock();
    }

    void ToggleDock()
    {
        docked = !docked; 
    }
    // public void ToggleDock (bool dockState)
    // {
    //     docked = dockState;

    //     if (docked)
    //     {
    //         // Stop movement and switch to kinematic mode (physics no longer controls it)
    //         rb.isKinematic = true;
    //         rb.linearVelocity = Vector3.zero;
    //         rb.angularVelocity = Vector3.zero;
    //     }
    //     else
    //     {
    //         // Resume physics mode
    //         rb.isKinematic = false;
    //         // The rotation smoothing logic can be removed now that we use AddTorque
    //     }
        
    //     // ... optional cursor lock logic ...
    // }
}
