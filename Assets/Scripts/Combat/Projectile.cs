using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 26f;
    [SerializeField] private float lifeSeconds = 3f;
    [SerializeField] private GameObject impactEffect;

    private Rigidbody rb;
    private GameObject owner;
    private float damage;
    private bool spent;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        Destroy(gameObject, lifeSeconds);
    }

    public void Launch(GameObject shooter, float amount)
    {
        owner = shooter;
        damage = amount;
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.forward * (speed * Time.fixedDeltaTime));
    }

    void OnTriggerEnter(Collider other)
    {
        if (spent) { return; }
        if (owner != null && other.transform.IsChildOf(owner.transform)) { return; }

        Damageable target = other.GetComponentInParent<Damageable>();
        if (target == null && other.isTrigger) { return; }

        if (target != null) { target.ApplyDamage(damage); }
        Hit();
    }

    private void Hit()
    {
        spent = true;
        if (impactEffect != null) { Instantiate(impactEffect, transform.position, Quaternion.identity); }
        Destroy(gameObject);
    }
}
