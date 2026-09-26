using UnityEngine;

public static class ExchangeService
{
    // swaps player and target colours as one step; enemies can only be used once
    public static bool TryExchange(ColorState player, ColorTarget target)
    {
        if (!ColorRules.CanExchange(player.Value, target.State.Value, target.Spent)) { return false; }

        ColorId mine = player.Value;
        ColorId theirs = target.State.Value;

        ColorContactPolicy.BeginBatch();
        player.Set(theirs);
        target.State.Set(mine);
        if (target.Kind == TargetKind.Enemy) { target.SetSpent(true); }
        ColorContactPolicy.EndBatch();

        Physics.SyncTransforms();
        return true;
    }
}
