using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;

    private Rigidbody rb;
    private Camera cam;
    private Vector3 facing = Vector3.forward;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        facing = transform.forward;
    }

    void Update()
    {
        AimAtCursor();

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void FixedUpdate()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) { input.Normalize(); }

        Vector3 flat = rb.linearVelocity;
        flat.y = 0f;

        Vector3 stepped = Vector3.MoveTowards(flat, input * moveSpeed, acceleration * Time.fixedDeltaTime);
        stepped.y = rb.linearVelocity.y;
        rb.linearVelocity = stepped;

        Quaternion wanted = Quaternion.LookRotation(facing, Vector3.up);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, wanted, turnSpeed * Time.fixedDeltaTime));
    }

    // the cursor ray meets a flat plane at the player's height, so aim ignores anything in the way
    private void AimAtCursor()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, transform.position);
        if (!ground.Raycast(ray, out float distance)) { return; }

        Vector3 look = ray.GetPoint(distance) - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.01f) { facing = look.normalized; }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || bulletSpawnPoint == null) { return; }
        Instantiate(bulletPrefab, bulletSpawnPoint.position, transform.rotation);
    }
}
