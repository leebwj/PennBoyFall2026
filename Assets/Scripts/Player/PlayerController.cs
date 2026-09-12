using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float acceleration = 70f;
    [SerializeField] private float turnDegreesPerSecond = 900f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashSeconds = 0.16f;
    [SerializeField] private float dashCooldownSeconds = 0.8f;

    [Header("Aim")]
    [SerializeField] private bool aimAtCursor = true;
    [SerializeField] private float gamepadAimDeadzone = 0.25f;

    private Rigidbody rb;
    private Camera cam;
    private InputAction moveAction;
    private InputAction pointAction;
    private InputAction aimAction;
    private InputAction dashAction;
    private InputAction sprintAction;

    private CooldownTimer dashCooldown;
    private float dashRemaining;
    private Vector2 moveInput;
    private Vector3 facing = Vector3.forward;

    public bool IsDashing => dashRemaining > 0f;
    public bool IsSprinting => sprintAction != null && sprintAction.IsPressed() && moveInput.sqrMagnitude > 0.01f;
    public float DashCharge => dashCooldown != null ? dashCooldown.Fraction : 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        dashCooldown = new CooldownTimer(dashCooldownSeconds);

        moveAction = InputRef.Find("Player/Move", this);
        pointAction = InputRef.Find("Player/Point", this);
        aimAction = InputRef.Find("Player/Aim", this);
        dashAction = InputRef.Find("Player/Dash", this);
        sprintAction = InputRef.Find("Player/Sprint", this);

        enabled = moveAction != null && dashAction != null;
    }

    void Update()
    {
        dashCooldown.Tick(Time.deltaTime);
        moveInput = moveAction.ReadValue<Vector2>();

        if (dashAction.WasPressedThisFrame() && !IsDashing && dashCooldown.TryUse())
        {
            dashRemaining = dashSeconds;
        }

        UpdateFacing();
    }

    void FixedUpdate()
    {
        if (dashRemaining > 0f)
        {
            dashRemaining -= Time.fixedDeltaTime;
            SetPlanarVelocity(facing * dashSpeed);
        }
        else
        {
            Vector3 wanted = new Vector3(moveInput.x, 0f, moveInput.y);
            if (wanted.sqrMagnitude > 1f) { wanted.Normalize(); }

            float speed = moveSpeed * (IsSprinting ? sprintMultiplier : 1f);
            Vector3 planar = rb.linearVelocity;
            planar.y = 0f;
            SetPlanarVelocity(Vector3.MoveTowards(planar, wanted * speed, acceleration * Time.fixedDeltaTime));
        }

        Quaternion wantedRotation = Quaternion.LookRotation(facing, Vector3.up);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, wantedRotation, turnDegreesPerSecond * Time.fixedDeltaTime));
    }

    private void UpdateFacing()
    {
        Vector2 stick = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;
        if (stick.sqrMagnitude > gamepadAimDeadzone * gamepadAimDeadzone)
        {
            facing = new Vector3(stick.x, 0f, stick.y).normalized;
            return;
        }

        if (aimAtCursor && cam != null && pointAction != null)
        {
            if (TryGetCursorGroundPoint(pointAction.ReadValue<Vector2>(), out Vector3 aim))
            {
                Vector3 toAim = aim - transform.position;
                toAim.y = 0f;
                if (toAim.sqrMagnitude > 0.04f) { facing = toAim.normalized; }
            }
            return;
        }

        if (moveInput.sqrMagnitude > 0.01f)
        {
            facing = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }
    }

    // intersects the cursor ray with a horizontal plane at player height so aiming ignores walls and props
    private bool TryGetCursorGroundPoint(Vector2 screenPoint, out Vector3 point)
    {
        Ray ray = cam.ScreenPointToRay(screenPoint);
        Plane ground = new Plane(Vector3.up, transform.position);
        if (ground.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }
        point = Vector3.zero;
        return false;
    }

    private void SetPlanarVelocity(Vector3 planar)
    {
        planar.y = rb.linearVelocity.y;
        rb.linearVelocity = planar;
    }
}
