using UnityEngine;

public class Bullet : MonoBehaviour
{
    // Initial speed of bullet
    [SerializeField] private float speed = 20f;

    // Life time of the spawned bullet
    [SerializeField] private float lifeSeconds = 3f;

    // Remember color state of the shooter
    private Colorable shooter;

    // Bullet's own color copied from shooter
    private GameColor shotColor;

    // Set spawned bullet's color
    public void Launch(Colorable from) {
        
        shooter = from;
        shotColor = from.CurrentColor; // Read shooter's color and set bullet color the same
    }

    // Spawn and start bullet timer
    void Start() {

        // Delete GameObject after set life seconds
        Destroy(gameObject, lifeSeconds);

    }

    // Move bullet forward
    void Update()
    {
        // Translate bullet forward the way its faced initially
        transform.Translate(Vector3.forward * (speed * Time.deltaTime));
    }

    // Triggerd when bullet collides something
    void OnTriggerEnter(Collider other)
    {

        // Ensure nothing happens when bullet somehow hits the player (transform defines the physical state of the object)
        if (other.transform.IsChildOf(shooter.transform)) {
            return;
        }

        // Check if the collided object is color switchable
        Colorable hit = other.GetComponentInParent<Colorable>();

        // When bullet collides null, destroy the bullet
        if (hit == null) {
            Destroy(gameObject);
            return;
        }

        // Nothing happens if the same color object is hit
        if (hit.CurrentColor == shotColor) {
            return;
        }

        // Otherwise bullet collides into opposite color, swap color
        GameColor theirs = hit.CurrentColor;

        hit.SetColor(shotColor); // Collided object gets the player's color
        shooter.SetColor(theirs); // Set player color  to the collided object's color

        Destroy(gameObject);
    }
}
