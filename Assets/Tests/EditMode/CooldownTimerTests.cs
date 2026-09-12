using System;
using NUnit.Framework;

public class CooldownTimerTests
{
    [Test]
    public void StartsReady()
    {
        var timer = new CooldownTimer(1f);

        Assert.IsTrue(timer.IsReady);
        Assert.AreEqual(0f, timer.Remaining);
    }

    [Test]
    public void UsingBlocksUntilTheFullDurationElapses()
    {
        var timer = new CooldownTimer(1f);

        Assert.IsTrue(timer.TryUse());
        Assert.IsFalse(timer.IsReady);
        Assert.IsFalse(timer.TryUse());

        timer.Tick(0.6f);
        Assert.IsFalse(timer.IsReady);

        timer.Tick(0.4f);
        Assert.IsTrue(timer.IsReady);
        Assert.IsTrue(timer.TryUse());
    }

    [Test]
    public void TickNeverDrivesRemainingBelowZero()
    {
        var timer = new CooldownTimer(0.5f);
        timer.TryUse();

        timer.Tick(10f);

        Assert.AreEqual(0f, timer.Remaining);
        Assert.IsTrue(timer.IsReady);
    }

    [Test]
    public void FractionReportsRechargeProgress()
    {
        var timer = new CooldownTimer(2f);
        timer.TryUse();

        Assert.AreEqual(0f, timer.Fraction, 0.0001f);

        timer.Tick(1f);
        Assert.AreEqual(0.5f, timer.Fraction, 0.0001f);

        timer.Tick(1f);
        Assert.AreEqual(1f, timer.Fraction, 0.0001f);
    }

    [Test]
    public void ZeroLengthTimerIsAlwaysReady()
    {
        var timer = new CooldownTimer(0f);

        Assert.IsTrue(timer.TryUse());
        Assert.IsTrue(timer.IsReady);
        Assert.AreEqual(1f, timer.Fraction);
    }

    [Test]
    public void ClearMakesTheTimerReadyImmediately()
    {
        var timer = new CooldownTimer(5f);
        timer.TryUse();

        timer.Clear();

        Assert.IsTrue(timer.IsReady);
    }

    [Test]
    public void RejectsOutOfContractInput()
    {
        var timer = new CooldownTimer(1f);

        Assert.Throws<ArgumentOutOfRangeException>(() => new CooldownTimer(-1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => timer.Tick(-0.5f));
    }
}
