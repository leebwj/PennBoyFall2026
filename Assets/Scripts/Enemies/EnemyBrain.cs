using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Damageable))]
public class EnemyBrain : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.4f;
    [SerializeField] private float sightRange = 18f;
    [SerializeField] private float stopDistance = 1.1f;
    [SerializeField] private float contactDamage = 12f;
    [SerializeField] private float contactIntervalSeconds = 0.8f;
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private GameObject deathEffect;

    private Rigidbody rb;
    private Damageable damageable;
    private Transform target;
    private CooldownTimer contactCooldown;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        damageable = GetComponent<Damageable>();
        contactCooldown = new CooldownTimer(contactIntervalSeconds);
    }

    void OnEnable()
    {
        damageable.Died += OnDied;
    }

    void OnDisable()
    {
        damageable.Died -= OnDied;
    }

    void Start()
    {
        PlayerController player = GameManager.Instance != null
            ? GameManager.Instance.Player
            : FindFirstObjectByType<PlayerController>();
        if (player != null) { target = player.transform; }
    }

    void FixedUpdate()
    {
        contactCooldown.Tick(Time.fixedDeltaTime);
        if (target == null) { return; }

        Vector3 toTarget = target.position - rb.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        Vector3 planar = Vector3.zero;
        if (distance < sightRange && distance > stopDistance)
        {
            planar = toTarget / distance * moveSpeed;
        }
        planar.y = rb.linearVelocity.y;
        rb.linearVelocity = planar;

        if (distance > 0.01f)
        {
            rb.MoveRotation(Quaternion.LookRotation(toTarget / distance, Vector3.up));
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) { return; }
        if (!contactCooldown.TryUse()) { return; }

        Damageable victim = collision.gameObject.GetComponentInParent<Damageable>();
        if (victim != null) { victim.ApplyDamage(contactDamage); }
    }

    private void OnDied(Damageable source)
    {
        if (deathEffect != null) { Instantiate(deathEffect, transform.position, Quaternion.identity); }
        if (GameManager.Instance != null) { GameManager.Instance.AddScore(scoreValue); }
        Destroy(gameObject);
    }
}
