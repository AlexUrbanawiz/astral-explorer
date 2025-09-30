using UnityEngine;

public class CameraTrack : MonoBehaviour
{
    [SerializeField] Transform cameraPos;
    void Update()
    {
        transform.position = cameraPos.position; 
    }
}
