using System.Collections.Generic;
using UnityEngine;

// same colour player and target collide, opposite colours ignore each other.
// works per collider pair so the gun can still hit a wall the body passes through
public static class ColorContactPolicy
{
    private static readonly Dictionary<Collider, ColorTarget> owners = new Dictionary<Collider, ColorTarget>();
    private static ColorState player;
    private static Collider[] playerBodies = new Collider[0];
    private static int batchDepth;
    private static bool dirty;

    public static void RegisterPlayer(ColorState state, Collider[] bodies)
    {
        player = state;
        playerBodies = bodies;
        ColorState.AnyChanged -= OnAnyChanged;
        ColorState.AnyChanged += OnAnyChanged;
        RefreshAll();
    }

    public static void Register(ColorTarget target)
    {
        foreach (Collider c in target.Bodies) { owners[c] = target; }
        Apply(target);
    }

    public static void Unregister(ColorTarget target)
    {
        foreach (Collider c in target.Bodies) { owners.Remove(c); }
    }

    public static ColorTarget Owner(Collider collider)
    {
        return collider != null && owners.TryGetValue(collider, out ColorTarget target) ? target : null;
    }

    // true when the player should treat this collider as something it can stand on or bump into
    public static bool IsSolidForPlayer(Collider collider)
    {
        ColorTarget target = Owner(collider);
        return target == null || player == null || ColorRules.IsSolid(player.Value, target.State.Value);
    }

    // group several colour changes so pairs are only updated once both sides have swapped
    public static void BeginBatch()
    {
        batchDepth++;
    }

    public static void EndBatch()
    {
        batchDepth = Mathf.Max(0, batchDepth - 1);
        if (batchDepth == 0 && dirty) { RefreshAll(); }
    }

    private static void OnAnyChanged(ColorState source)
    {
        if (batchDepth > 0)
        {
            dirty = true;
            return;
        }
        RefreshAll();
    }

    private static void RefreshAll()
    {
        dirty = false;
        foreach (ColorTarget target in ColorTarget.All) { Apply(target); }
    }

    private static void Apply(ColorTarget target)
    {
        if (player == null) { return; }
        bool ignore = !ColorRules.IsSolid(player.Value, target.State.Value);
        foreach (Collider mine in playerBodies)
        {
            foreach (Collider theirs in target.Bodies)
            {
                Physics.IgnoreCollision(mine, theirs, ignore);
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        owners.Clear();
        player = null;
        playerBodies = new Collider[0];
        batchDepth = 0;
        dirty = false;
    }
}
