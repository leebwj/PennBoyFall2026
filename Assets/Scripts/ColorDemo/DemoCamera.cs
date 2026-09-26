using UnityEngine;

// orthographic follow camera: yaw 45, pitch 35, eases wider in elevated encounters
[RequireComponent(typeof(Camera))]
public class DemoCamera : MonoBehaviour
{
    [SerializeField] private DemoPlayer target;
    [SerializeField] private float yaw = 45f;
    [SerializeField] private float pitch = 35f;
    [SerializeField] private float distance = 40f;
    [SerializeField] private float followSeconds = 0.2f;
    [SerializeField] private float lead = 1.5f;
    [SerializeField] private float normalSize = 10f;
    [SerializeField] private float elevatedSize = 14f;
    [SerializeField] private float sizeSeconds = 0.8f;

    private Camera cam;
    private Vector3 velocity;
    private float sizeVelocity;

    public void Configure(DemoPlayer player)
    {
        target = player;
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = normalSize;
    }

    void Start()
    {
        if (target == null) { return; }
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = Wanted();
    }

    void LateUpdate()
    {
        if (target == null) { return; }
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = Vector3.SmoothDamp(transform.position, Wanted(), ref velocity, followSeconds);

        ColorRegion active = DemoDirector.Instance != null ? DemoDirector.Instance.Active : null;
        float size = active != null && active.Elevated ? elevatedSize : normalSize;
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, size, ref sizeVelocity, sizeSeconds);
    }

    private Vector3 Wanted()
    {
        Vector3 focus = target.transform.position + Vector3.up * 0.8f + target.Facing * lead;
        return focus - transform.forward * distance;
    }
}
