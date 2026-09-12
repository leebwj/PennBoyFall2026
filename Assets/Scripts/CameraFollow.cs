using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -8f);
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float lookAhead = 2.5f;

    private Vector3 velocity;

    void Start()
    {
        if (target == null) { return; }
        transform.position = FollowPoint() + offset;
        transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
    }

    void LateUpdate()
    {
        if (target == null) { return; }
        transform.position = Vector3.SmoothDamp(transform.position, FollowPoint() + offset, ref velocity, smoothTime);
    }

    // shifting the point the camera follows toward where the player aims shows more of what is ahead
    private Vector3 FollowPoint()
    {
        return target.position + target.forward * lookAhead;
    }
}
