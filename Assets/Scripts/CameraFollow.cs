using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    // Update is called once per frame
    void LateUpdate()
    {
        if (player == null) return;

        Vector3 target = player.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, target, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
