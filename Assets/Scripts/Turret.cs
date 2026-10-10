using UnityEngine;

[RequireComponent(typeof(Colorable))]
// Turret requires color like the source
public class Turret : MonoBehaviour {

    // Projectible prefab cloned for each shot
    [SerializeField] private GameObject shotPrefab;

    // Spawn point of shot
    [SerializeField] private Transform shotSpawnPoint;

    // Object shown only while turret is warning state
    [SerializeField] private GameObject warningLight;

    // Firing cycle (Warn 2 / Fire 1 / Quiet 7)
    [SerializeField] private float warnSeconds = 2f;
    [SerializeField] private float fireSeconds = 1f;
    [SerializeField] private float quietSeconds = 7f;

    // Interval seconds between shots when firing
    [SerializeField] private float shotInterval = 0.2f;

    private Colorable colorable; // Turret color
    private float cycleTime; // How far into current cycle the state is at
    private float nextShotTime; // Time when next shot allowed
    private bool spent; // True after its one color exchange

    public bool Spent => spent; // Read only so that Bullet can check if exchange is used


    void Start() {
        colorable = GetComponent<Colorable>();

        if (warningLight != null) {
            warningLight.SetActive(false);
        }
    } 

    void Update() {

        // Count up and go back to 0 when cycle ends
        cycleTime += Time.deltaTime;
        float cycleLength = warnSeconds + fireSeconds + quietSeconds;

        if (cycleTime >= cycleLength) {
            cycleTime -= cycleLength;
        }

        // Figure which part of cycle currently at
        bool warning = cycleTime < warnSeconds;
        bool firing = !warning && cycleTime < warnSeconds + fireSeconds;

        // Show warn during warn cycle
        if (warningLight != null) {
            warningLight.SetActive(warning);
        }

        // Fire while in firing cycle
        if (firing && Time.time >= nextShotTime) {
            Fire();
            nextShotTime = Time.time + shotInterval;
        }
    }

    // Called by Bullet after one exchange happens
    public void MarkSpent() {
        spent = true;
    }

    // Spawn one shot with the turret's current color
    private void Fire() {
        GameObject shot = Instantiate(shotPrefab, shotSpawnPoint.position, shotSpawnPoint.rotation);
        shot.GetComponent<TurretShot>().Launch(transform, colorable.CurrentColor);
    }
}