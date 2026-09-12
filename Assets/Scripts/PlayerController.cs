using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private bool faceMouse = true;

    private Rigidbody rb;
    private Camera cam;
    private InputAction moveAction;
    private Vector2 moveInput;
    private Vector3 facing = Vector3.forward;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        moveAction = InputSystem.actions.FindAction("Player/Move");
    }

    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        if (faceMouse && cam != null && Mouse.current != null)
        {
            Vector3 aim;
            if (TryGetMouseGroundPoint(out aim))
            {
                Vector3 toAim = aim - transform.position;
                toAim.y = 0f;
                if (toAim.sqrMagnitude > 0.01f) { facing = toAim.normalized; }
            }
        }
        else if (moveInput.sqrMagnitude > 0.01f)
        {
            facing = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }
    }

    void FixedUpdate()
    {
        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (dir.sqrMagnitude > 1f) { dir.Normalize(); }

        Vector3 velocity = dir * moveSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;

        Quaternion wanted = Quaternion.LookRotation(facing, Vector3.up);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, wanted, turnSpeed * Time.fixedDeltaTime));
    }

    // intersects the cursor ray with a horizontal plane at player height so aiming ignores colliders
    private bool TryGetMouseGroundPoint(out Vector3 point)
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        var plane = new Plane(Vector3.up, transform.position);
        float dist;
        if (plane.Raycast(ray, out dist))
        {
            point = ray.GetPoint(dist);
            return true;
        }
        point = Vector3.zero;
        return false;
    }
}
