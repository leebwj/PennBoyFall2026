using UnityEngine;
using UnityEngine.SceneManagement;

public class TurretShot : MonoBehaviour {

    // Speed of shot
    [SerializeField] private float speed = 8f;

    // Life time of spawned bullet
    [SerializeField] private float lifeSeconds = 4f;

    private Transform owner; // Turret that fired the shot
    private GameColor shotColor; // Bullet current color

    // Set up shot right after spawned
    public void Launch(Transform from, GameColor color) {
        
        owner = from;
        shotColor = color;

        // Paint shot its respective color
        Color tint = Colorable.Tint(color);
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) {
            r.material.color = tint;
        }
    }

    void Start() {
        Destroy(gameObject, lifeSeconds);
    }

    void Update() {
        transform.Translate(Vector3.forward * (speed * Time.deltaTime));
    }

    void OnTriggerEnter(Collider other) {

        // Ignore turret that fired it
        if (other.transform.IsChildOf(owner)) {
            return;
        }

        // Hit player. Damage Player only when colors match. Otherwise pass through
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null) {
            if (player.GetComponent<Colorable>().CurrentColor == shotColor) {
                // Restart current level
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            return;
        }

        // Grey walls no effect. 
        if (other.GetComponentInParent<Colorable>() == null) {
            Destroy(gameObject);
        }
    }
}