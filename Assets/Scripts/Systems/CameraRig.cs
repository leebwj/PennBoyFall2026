using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRig : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private float height = 17f;
    [SerializeField] private float distance = 11f;
    [SerializeField] private float smoothTime = 0.14f;
    [SerializeField] private float aimLead = 2.5f;

    [Header("Zoom")]
    [SerializeField] private float minHeight = 9f;
    [SerializeField] private float maxHeight = 28f;
    [SerializeField] private float zoomStep = 1.5f;

    [Header("Bounds")]
    [SerializeField] private Vector2 focusMin = new Vector2(-24f, -24f);
    [SerializeField] private Vector2 focusMax = new Vector2(24f, 24f);

    private InputAction zoomAction;
    private Vector3 followVelocity;
    private float distanceRatio = 1f;
    private float shakeSeconds;
    private float shakeStrength;

    void Awake()
    {
        distanceRatio = height > 0f ? distance / height : 0.65f;
        zoomAction = InputRef.Find("Player/Zoom", this);
    }

    void Start()
    {
        if (target == null) { return; }
        transform.position = FocusToCamera(Focus());
        transform.rotation = Quaternion.LookRotation(-Offset().normalized, Vector3.up);
    }

    void LateUpdate()
    {
        if (target == null) { return; }

        ApplyZoom();

        Vector3 wanted = FocusToCamera(Focus());
        transform.position = Vector3.SmoothDamp(transform.position, wanted, ref followVelocity, smoothTime) + ShakeOffset();
        transform.rotation = Quaternion.LookRotation(-Offset().normalized, Vector3.up);
    }

    public void Shake(float strength, float seconds)
    {
        if (strength <= shakeStrength && shakeSeconds > 0f) { return; }
        shakeStrength = strength;
        shakeSeconds = seconds;
    }

    private void ApplyZoom()
    {
        if (zoomAction == null) { return; }
        float scroll = zoomAction.ReadValue<float>();
        if (Mathf.Abs(scroll) < 0.01f) { return; }
        height = Mathf.Clamp(height - Mathf.Sign(scroll) * zoomStep, minHeight, maxHeight);
    }

    private Vector3 Focus()
    {
        Vector3 focus = target.position + target.forward * aimLead;
        focus.x = Mathf.Clamp(focus.x, focusMin.x, focusMax.x);
        focus.z = Mathf.Clamp(focus.z, focusMin.y, focusMax.y);
        focus.y = target.position.y;
        return focus;
    }

    private Vector3 Offset()
    {
        return new Vector3(0f, height, -height * distanceRatio);
    }

    private Vector3 FocusToCamera(Vector3 focus)
    {
        return focus + Offset();
    }

    private Vector3 ShakeOffset()
    {
        if (shakeSeconds <= 0f) { return Vector3.zero; }
        shakeSeconds -= Time.deltaTime;
        float falloff = Mathf.Max(0f, shakeSeconds);
        return Random.insideUnitSphere * (shakeStrength * falloff);
    }
}
