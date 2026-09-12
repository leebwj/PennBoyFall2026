using System;
using NUnit.Framework;

public class HealthPoolTests
{
    [Test]
    public void StartsFull()
    {
        var pool = new HealthPool(100f);

        Assert.AreEqual(100f, pool.Current);
        Assert.AreEqual(1f, pool.Fraction);
        Assert.IsFalse(pool.IsDead);
    }

    [Test]
    public void DamageReducesCurrentAndReportsAmountTaken()
    {
        var pool = new HealthPool(100f);

        float taken = pool.Damage(30f);

        Assert.AreEqual(30f, taken);
        Assert.AreEqual(70f, pool.Current);
        Assert.AreEqual(0.7f, pool.Fraction, 0.0001f);
    }

    [Test]
    public void DamageStopsAtZeroAndMarksDead()
    {
        var pool = new HealthPool(50f);

        float taken = pool.Damage(80f);

        Assert.AreEqual(50f, taken);
        Assert.AreEqual(0f, pool.Current);
        Assert.IsTrue(pool.IsDead);
    }

    [Test]
    public void DamageOnDeadPoolDoesNothing()
    {
        var pool = new HealthPool(10f);
        pool.Damage(10f);

        Assert.AreEqual(0f, pool.Damage(5f));
        Assert.AreEqual(0f, pool.Current);
    }

    [Test]
    public void HealNeverExceedsMax()
    {
        var pool = new HealthPool(100f);
        pool.Damage(20f);

        float given = pool.Heal(50f);

        Assert.AreEqual(20f, given);
        Assert.AreEqual(100f, pool.Current);
    }

    [Test]
    public void HealOnDeadPoolDoesNothing()
    {
        var pool = new HealthPool(30f);
        pool.Damage(30f);

        Assert.AreEqual(0f, pool.Heal(15f));
        Assert.IsTrue(pool.IsDead);
    }

    [Test]
    public void RefillRestoresADeadPool()
    {
        var pool = new HealthPool(80f);
        pool.Damage(100f);

        pool.Refill();

        Assert.AreEqual(80f, pool.Current);
        Assert.IsFalse(pool.IsDead);
    }

    [Test]
    public void RejectsOutOfContractInput()
    {
        var pool = new HealthPool(10f);

        Assert.Throws<ArgumentOutOfRangeException>(() => new HealthPool(0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => pool.Damage(-1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => pool.Heal(-1f));
    }
}
