using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;

    float xRotation;
    float yRotation;

    public InputActionAsset actions;
    InputAction lookAction;
    Vector2 lookValue;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        lookAction = actions.FindAction("Look");
    }

    // Update is called once per frame
    void Update()
    {
        lookValue = lookAction.ReadValue<Vector2>();
        float xMouse = lookValue.x * Time.deltaTime * sensX; 
        float yMouse = lookValue.y * Time.deltaTime * sensY;

        yRotation += xMouse;
        xRotation -= yMouse;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);

        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
