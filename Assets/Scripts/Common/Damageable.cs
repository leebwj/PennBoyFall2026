using System;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float hitIntervalSeconds = 0.1f;

    public event Action<Damageable> Died = delegate { };

    private HealthPool pool;
    private CooldownTimer hitGate;

    public float Max => maxHealth;
    public float Current => pool != null ? pool.Current : maxHealth;
    public float Fraction => pool != null ? pool.Fraction : 1f;
    public bool IsDead => pool != null && pool.IsDead;

    void Awake()
    {
        pool = new HealthPool(maxHealth);
        hitGate = new CooldownTimer(hitIntervalSeconds);
    }

    void Update()
    {
        hitGate.Tick(Time.deltaTime);
    }

    public void ApplyDamage(float amount)
    {
        if (pool.IsDead || amount <= 0f) { return; }
        if (!hitGate.TryUse()) { return; }

        pool.Damage(amount);
        if (pool.IsDead) { Died(this); }
    }

    public void Heal(float amount)
    {
        pool.Heal(amount);
    }

    public void Revive()
    {
        pool.Refill();
        hitGate.Clear();
    }
}
