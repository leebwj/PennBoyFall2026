using UnityEngine;

// Player object automatically gets RigidBody
[RequireComponent(typeof(Rigidbody))]

public class PlayerController : MonoBehaviour
{
    // Speed of player movement
    [SerializeField] private float moveSpeed = 6f;

    // Acceleration of the player. Increase to make it more reactive
    [SerializeField] private float acceleration = 40f;

    // Speed of player following mouse rotation in degrees per second
    [SerializeField] private float turnSpeed = 720f;

    // Bullet prefab object to fire out of player. Cloned on each shot
    [SerializeField] private GameObject bulletPrefab;

    // Position of where bullets spawn. Initially in front of player.
    [SerializeField] private Transform bulletSpawnPoint;

    
    private Rigidbody rb; // Player physics body
    private Camera cam; // Scene camera
    private Vector3 facing = Vector3.forward; // Direction player is facing forward

    // Initial state of player
    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Find initial RigidBody
        cam = Camera.main; // Find initial scene camera
        facing = transform.forward; // Face whereever the object was placed in scene initially 
    }

    // Update basic player movement every frame
    void Update()
    {
        // Find where the mouse is currently located and face towards it
        AimAtCursor();

        // If clicked, trigger bullet shot
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    // Update player physics movement
    void FixedUpdate()
    {
        // Read and save WASD direction.
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        // Change key direction to match camera view point
        input = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f) * input;
        
        // Block diagonal movement from getting faster than normal movement.
        if (input.sqrMagnitude > 1f) { 
            input.Normalize(); // Shrink speed to 1. 
        }

        // Take velocity, and set y to 0 so movement doesn't fight gravity and act instant
        Vector3 flat = rb.linearVelocity;
        flat.y = 0f;

        // Speed player up or slow down gradually
        Vector3 stepped = Vector3.MoveTowards(flat, input * moveSpeed, acceleration * Time.fixedDeltaTime);
        
        // Restore y influence to eventually stop
        stepped.y = rb.linearVelocity.y;
        rb.linearVelocity = stepped;

        // Set player to turn not faster than turnSpeed degrees per second
        Quaternion wanted = Quaternion.LookRotation(facing, Vector3.up);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, wanted, turnSpeed * Time.fixedDeltaTime));
    }

    // Check where the mouse is pointing at and store in "facing"
    private void AimAtCursor()
    {
        // Draw line from camera through the mouse cursor out into world
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Make invisible floor at player's height where line lands on
        Plane ground = new Plane(Vector3.up, transform.position);

        // Find where line landed. If mouse never is on floor then keep old facing
        if (!ground.Raycast(ray, out float distance)) {
             return; 
        }

        // Otherwise update player facing direction to the cursor point
        Vector3 look = ray.GetPoint(distance) - transform.position;
        
        look.y = 0f; // Flatten direction so player doesn't tilt

        // If direction too short normalize shrink to 1.
        if (look.sqrMagnitude > 0.01f) { 
            facing = look.normalized; 
        } 
    }

    // Fire bullet on every click
    private void Shoot()
    {
        // Return nothing if scene setup forgets bullet setup
        if (bulletPrefab == null || bulletSpawnPoint == null) { 
            return; 
        }

        // Create bullet by cloning prefab at spawn point. 
        GameObject shot = Instantiate(bulletPrefab, bulletSpawnPoint.position, transform.rotation);

        // Remember the color of the player.
        shot.GetComponent<Bullet>().Launch(GetComponent<Colorable>());
    }
}
