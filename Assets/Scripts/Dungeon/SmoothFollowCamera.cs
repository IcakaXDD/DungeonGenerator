using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    public Transform target; // The character or object the camera will follow
    public float smoothSpeed = 0.125f; // Adjust for smoother or faster transitions
    private Vector3 offset; // The initial offset between the camera and the target

    void Start()
    {
        // Calculate the initial offset at the start
        if (target != null)
        {
            offset = transform.position - target.position;
        }
        else
        {
            Debug.LogError("Target not assigned in SmoothFollowCamera script.");
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Calculate the desired camera position
            Vector3 desiredPosition = target.position + offset;

            // Smoothly interpolate to the desired position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        }
    }
}