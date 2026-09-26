using NUnit.Framework;

public class ColorRulesTests
{
    [Test]
    public void OppositeColoursPassSameColoursCollide()
    {
        Assert.IsTrue(ColorRules.IsSolid(ColorId.Red, ColorId.Red));
        Assert.IsFalse(ColorRules.IsSolid(ColorId.Red, ColorId.Blue));
        Assert.AreEqual(ColorId.Blue, ColorRules.Opposite(ColorId.Red));
    }

    [Test]
    public void ExchangeNeedsDifferentColoursAndAnUnspentTarget()
    {
        Assert.IsTrue(ColorRules.CanExchange(ColorId.Red, ColorId.Blue, false));
        Assert.IsFalse(ColorRules.CanExchange(ColorId.Red, ColorId.Red, false));
        Assert.IsFalse(ColorRules.CanExchange(ColorId.Red, ColorId.Blue, true));
    }

    [Test]
    public void MatchingWallTransmitsAndOppositeTargetBehindItExchanges()
    {
        var hits = new[]
        {
            new ShotHit(ShotHitKind.Wall, ColorId.Blue),
            new ShotHit(ShotHitKind.Wall, ColorId.Red),
        };
        ShotResult result = ShotRules.Resolve(ColorId.Blue, hits);
        Assert.AreEqual(1, result.StopIndex);
        Assert.IsTrue(result.Exchanges);
    }

    [Test]
    public void NormalGeometryStopsTheShot()
    {
        var hits = new[]
        {
            new ShotHit(ShotHitKind.Normal),
            new ShotHit(ShotHitKind.Target, ColorId.Blue),
        };
        ShotResult result = ShotRules.Resolve(ColorId.Red, hits);
        Assert.AreEqual(0, result.StopIndex);
        Assert.IsFalse(result.Exchanges);
    }

    [Test]
    public void MatchingObjectStopsWithoutExchange()
    {
        ShotResult result = ShotRules.Resolve(ColorId.Red, new[] { new ShotHit(ShotHitKind.Target, ColorId.Red) });
        Assert.AreEqual(0, result.StopIndex);
        Assert.IsFalse(result.Exchanges);
    }

    [Test]
    public void SpentEnemyStopsTheShotButCannotExchange()
    {
        ShotResult result = ShotRules.Resolve(ColorId.Red, new[] { new ShotHit(ShotHitKind.Target, ColorId.Blue, true) });
        Assert.AreEqual(0, result.StopIndex);
        Assert.IsFalse(result.Exchanges);
    }

    [Test]
    public void EmptyRayRunsFullRange()
    {
        Assert.AreEqual(-1, ShotRules.Resolve(ColorId.Red, new ShotHit[0]).StopIndex);
    }
}
