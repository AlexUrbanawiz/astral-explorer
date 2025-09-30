using System;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public InputActionAsset actionMap;
    public InputAction moveAction = new InputAction();
    public float moveSpeed;

    public Vector3 moveDirection;
    public InputAction jumpButton;
    public Transform orientation;
    Rigidbody rb;
    public bool jumpReady = true;
    public bool grounded;
    public float jumpCooldown;
    public float jumpForce;
    public float groundDrag;
    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool jumpPressed;

    public float fuelMax;
    public float fuelCurrent;
    public float burnRate;
    public float jetpackStrength;
    public GameObject jetpackParticles;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = actionMap.FindAction("Move");
        jumpButton = actionMap.FindAction("Jump");
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        jumpReady = true;
        fuelCurrent = fuelMax;
        jetpackParticles.SetActive(false);
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    // Update is called once per frame
    void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, (playerHeight * .5f) + .02f, whatIsGround);
        GetInput();
        SpeedControl();

        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }
        transform.rotation = orientation.rotation;
    }



    void GetInput()
    {

        moveDirection = moveAction.ReadValue<Vector2>();
        jumpPressed = jumpButton.IsPressed();
        if (jumpPressed && grounded && jumpReady)
        {
            jumpReady = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        else if (jumpPressed)
        {
            UseJetpack();
        }

        if (!jumpPressed && !grounded && !jumpReady)
        { 
        if (jetpackParticles.activeSelf)
        {
            jetpackParticles.SetActive(false);
        }
        }
    }



    void MovePlayer()
    {
        moveDirection = orientation.forward * moveDirection.y + orientation.right * moveDirection.x;
        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }

    }

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        jumpReady = true;
    }

    void UseJetpack()
    {
        if (!jetpackParticles.activeSelf)
        { 
            jetpackParticles.SetActive(true);
        }
        if (fuelCurrent - burnRate >= 0)
        {
            fuelCurrent -= burnRate;
            rb.AddForce(transform.up * jetpackStrength, ForceMode.Force);
        }
    }

}
