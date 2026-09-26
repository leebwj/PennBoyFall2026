using UnityEngine;
using UnityEngine.InputSystem;

// grounding aware walker for the colour map. no jump, dash or sprint.
// the pivot sits at the feet, the capsule is 0.3 m radius and 1.6 m tall
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ColorState))]
public class DemoPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float maxSlopeDegrees = 30f;
    [SerializeField] private float turnDegreesPerSecond = 900f;

    [Header("Body")]
    [SerializeField] private float radius = 0.3f;
    [SerializeField] private float muzzleHeight = 1f;
    [SerializeField] private Transform visual;

    [Header("Aim")]
    [SerializeField] private float gamepadAimDeadzone = 0.25f;

    private const float ProbeLift = 0.05f;
    private const float ProbeReach = 0.35f;
    private const float FlatDegrees = 3f;
    private const float PressIntoGround = 0.6f;

    private readonly RaycastHit[] probeHits = new RaycastHit[16];

    private Rigidbody rb;
    private Camera cam;
    private InputAction moveAction;
    private InputAction pointAction;
    private InputAction aimAction;
    private Vector2 moveInput;
    private Vector3 facing = Vector3.forward;
    private Vector3 groundNormal = Vector3.up;
    private Collider[] bodies;

    public ColorState Color { get; private set; }
    public bool Grounded { get; private set; }
    public bool OnFlat { get; private set; }
    public float StandingY { get; private set; }
    public Vector3 Facing => facing;
    public Vector3 Muzzle => new Vector3(transform.position.x, StandingY + muzzleHeight, transform.position.z);
    public Collider[] Bodies => bodies;
    public float Radius => radius;

    public void Configure(Transform visualRoot)
    {
        visual = visualRoot;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Color = GetComponent<ColorState>();
        cam = Camera.main;
        bodies = GetComponentsInChildren<Collider>();
        StandingY = transform.position.y;

        var slick = new PhysicsMaterial("Frictionless")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
        };
        foreach (Collider c in bodies) { c.sharedMaterial = slick; }

        moveAction = InputRef.Find("Player/Move", this);
        pointAction = InputRef.Find("Player/Point", this);
        aimAction = InputRef.Find("Player/Aim", this);

        ColorContactPolicy.RegisterPlayer(Color, bodies);
        enabled = moveAction != null;
    }

    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        UpdateFacing();
        if (visual != null)
        {
            visual.rotation = Quaternion.RotateTowards(visual.rotation, Quaternion.LookRotation(facing, Vector3.up), turnDegreesPerSecond * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        Probe();

        Vector3 wanted = CameraRelative(moveInput) * walkSpeed;
        Vector3 velocity = rb.linearVelocity;

        if (Grounded)
        {
            // move along the surface and press lightly into it, so ramps neither launch nor slide the player
            rb.useGravity = false;
            Vector3 along = Vector3.ProjectOnPlane(wanted, groundNormal).normalized * wanted.magnitude;
            Vector3 current = Vector3.ProjectOnPlane(velocity, groundNormal);
            rb.linearVelocity = Vector3.MoveTowards(current, along, acceleration * Time.fixedDeltaTime) - groundNormal * PressIntoGround;
        }
        else
        {
            rb.useGravity = true;
            Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
            planar = Vector3.MoveTowards(planar, wanted, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(planar.x, velocity.y, planar.z);
        }
    }

    public void Teleport(Vector3 feet, ColorId color)
    {
        rb.linearVelocity = Vector3.zero;
        rb.position = feet;
        transform.position = feet;
        StandingY = feet.y;
        Color.Set(color);
        Physics.SyncTransforms();
    }

    // sphere cast from just above the feet, ignoring colliders the player currently phases through
    private void Probe()
    {
        Vector3 origin = rb.position + Vector3.up * (radius + ProbeLift);
        int count = Physics.SphereCastNonAlloc(origin, radius * 0.95f, Vector3.down, probeHits, ProbeLift + ProbeReach, ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        RaycastHit ground = default;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = probeHits[i];
            if (hit.distance <= 0f || IsMine(hit.collider)) { continue; }
            if (!ColorContactPolicy.IsSolidForPlayer(hit.collider)) { continue; }
            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeDegrees + 1f) { continue; }
            if (hit.distance < best)
            {
                best = hit.distance;
                ground = hit;
            }
        }

        Grounded = best < float.MaxValue;
        if (!Grounded)
        {
            groundNormal = Vector3.up;
            OnFlat = false;
            return;
        }

        groundNormal = ground.normal;
        StandingY = ground.point.y;
        OnFlat = Vector3.Angle(groundNormal, Vector3.up) < FlatDegrees && CentreIsOverFlat();
    }

    private bool CentreIsOverFlat()
    {
        Vector3 origin = rb.position + Vector3.up * 0.5f;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, probeHits, 0.5f + ProbeReach, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = probeHits[i];
            if (IsMine(hit.collider) || !ColorContactPolicy.IsSolidForPlayer(hit.collider)) { continue; }
            if (Vector3.Angle(hit.normal, Vector3.up) < FlatDegrees) { return true; }
        }
        return false;
    }

    private bool IsMine(Collider c)
    {
        foreach (Collider mine in bodies)
        {
            if (mine == c) { return true; }
        }
        return false;
    }

    private Vector3 CameraRelative(Vector2 input)
    {
        if (input.sqrMagnitude < 0.0001f) { return Vector3.zero; }
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (cam != null)
        {
            forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
        }
        Vector3 wanted = forward * input.y + right * input.x;
        return wanted.sqrMagnitude > 1f ? wanted.normalized : wanted;
    }

    private void UpdateFacing()
    {
        Vector2 stick = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;
        if (stick.sqrMagnitude > gamepadAimDeadzone * gamepadAimDeadzone)
        {
            facing = CameraRelative(stick).normalized;
            return;
        }

        if (cam != null && pointAction != null && Mouse.current != null)
        {
            // aim against the horizontal plane at muzzle height above the current footing
            Ray ray = cam.ScreenPointToRay(pointAction.ReadValue<Vector2>());
            Plane plane = new Plane(Vector3.up, Muzzle);
            if (plane.Raycast(ray, out float distance))
            {
                Vector3 toAim = ray.GetPoint(distance) - transform.position;
                toAim.y = 0f;
                if (toAim.sqrMagnitude > 0.04f) { facing = toAim.normalized; }
            }
            return;
        }

        Vector3 moving = CameraRelative(moveInput);
        if (moving.sqrMagnitude > 0.01f) { facing = moving.normalized; }
    }
}
