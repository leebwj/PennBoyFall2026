using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -8f);
    [SerializeField] private float smoothTime = 0.12f;

    private Vector3 velocity;

    void Start()
    {
        transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
        if (target != null) { transform.position = target.position + offset; }
    }

    void LateUpdate()
    {
        if (target == null) { return; }
        transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref velocity, smoothTime);
    }
}
