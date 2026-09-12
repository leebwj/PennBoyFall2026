using System;

public class HealthPool
{
    public float Max { get; private set; }
    public float Current { get; private set; }

    public bool IsDead => Current <= 0f;
    public float Fraction => Max > 0f ? Current / Max : 0f;

    public HealthPool(float max)
    {
        if (max <= 0f) { throw new ArgumentOutOfRangeException(nameof(max)); }
        Max = max;
        Current = max;
    }

    public float Damage(float amount)
    {
        if (amount < 0f) { throw new ArgumentOutOfRangeException(nameof(amount)); }
        if (IsDead) { return 0f; }
        float taken = Math.Min(amount, Current);
        Current -= taken;
        return taken;
    }

    public float Heal(float amount)
    {
        if (amount < 0f) { throw new ArgumentOutOfRangeException(nameof(amount)); }
        if (IsDead) { return 0f; }
        float given = Math.Min(amount, Max - Current);
        Current += given;
        return given;
    }

    public void Refill()
    {
        Current = Max;
    }
}
