using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    private void LateUpdate()
    {
        Vector3 desiredPosition = player.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
        transform.position = smoothedPosition;

        // You might want to adjust the camera's z-axis if it gets too close or too far from the scene
        transform.position = new Vector3(transform.position.x, transform.position.y, -10);
    }
}