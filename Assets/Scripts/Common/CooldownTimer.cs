using System;

public class CooldownTimer
{
    private readonly float length;
    private float remaining;

    public bool IsReady => remaining <= 0f;
    public float Remaining => remaining;
    public float Fraction => length > 0f ? 1f - remaining / length : 1f;

    public CooldownTimer(float seconds)
    {
        if (seconds < 0f) { throw new ArgumentOutOfRangeException(nameof(seconds)); }
        length = seconds;
    }

    public void Tick(float deltaSeconds)
    {
        if (deltaSeconds < 0f) { throw new ArgumentOutOfRangeException(nameof(deltaSeconds)); }
        remaining = Math.Max(0f, remaining - deltaSeconds);
    }

    public bool TryUse()
    {
        if (!IsReady) { return false; }
        remaining = length;
        return true;
    }

    public void Clear()
    {
        remaining = 0f;
    }
}
