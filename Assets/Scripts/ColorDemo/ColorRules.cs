using System.Collections.Generic;

public enum ColorId { Red = 0, Blue = 1 }

// plain rules with no Unity dependency so EditMode tests can call them directly
public static class ColorRules
{
    public static ColorId Opposite(ColorId color)
    {
        return color == ColorId.Red ? ColorId.Blue : ColorId.Red;
    }

    // same colour bodies collide, opposite colours pass through each other
    public static bool IsSolid(ColorId a, ColorId b)
    {
        return a == b;
    }

    public static bool CanExchange(ColorId shooter, ColorId target, bool spent)
    {
        return !spent && shooter != target;
    }
}

public enum ShotHitKind { Normal, Wall, Target }

public readonly struct ShotHit
{
    public readonly ShotHitKind Kind;
    public readonly ColorId Color;
    public readonly bool Spent;

    public ShotHit(ShotHitKind kind, ColorId color = ColorId.Red, bool spent = false)
    {
        Kind = kind;
        Color = color;
        Spent = spent;
    }
}

public readonly struct ShotResult
{
    // index of the hit that stopped the shot, or -1 when it ran its full range
    public readonly int StopIndex;
    public readonly bool Exchanges;

    public ShotResult(int stopIndex, bool exchanges)
    {
        StopIndex = stopIndex;
        Exchanges = exchanges;
    }
}

public static class ShotRules
{
    // hits must be sorted near to far. matching walls transmit the shot, normal geometry stops it,
    // and the first other target stops it and exchanges when the colours differ and it is not spent
    public static ShotResult Resolve(ColorId shooter, IReadOnlyList<ShotHit> hits)
    {
        for (int i = 0; i < hits.Count; i++)
        {
            ShotHit hit = hits[i];
            if (hit.Kind == ShotHitKind.Normal) { return new ShotResult(i, false); }
            if (hit.Kind == ShotHitKind.Wall && hit.Color == shooter) { continue; }
            return new ShotResult(i, ColorRules.CanExchange(shooter, hit.Color, hit.Spent));
        }
        return new ShotResult(-1, false);
    }
}
