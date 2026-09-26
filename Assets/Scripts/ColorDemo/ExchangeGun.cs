using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// horizontal exchange shot with a 12 m range. fires only while standing on flat ground,
// never from inside a wall or target, and resolves at most one exchange
[RequireComponent(typeof(DemoPlayer))]
public class ExchangeGun : MonoBehaviour
{
    [SerializeField] private float range = 12f;
    [SerializeField] private float cooldownSeconds = 0.3f;
    [SerializeField] private float beamSeconds = 0.14f;
    [SerializeField] private LineRenderer beam;
    [SerializeField] private LineRenderer guide;
    [SerializeField] private Color red = new Color(0.9f, 0.24f, 0.26f);
    [SerializeField] private Color blue = new Color(0.22f, 0.45f, 0.95f);
    [SerializeField] private Color blocked = new Color(0.6f, 0.6f, 0.6f);

    private readonly RaycastHit[] rayHits = new RaycastHit[48];
    private readonly Collider[] overlaps = new Collider[16];
    private readonly List<RaycastHit> sorted = new List<RaycastHit>();
    private readonly List<ShotHit> shotHits = new List<ShotHit>();
    private readonly List<ColorTarget> shotTargets = new List<ColorTarget>();

    private DemoPlayer player;
    private InputAction fireAction;
    private CooldownTimer cooldown;
    private float beamRemaining;

    public void Configure(LineRenderer beamLine, LineRenderer guideLine, Color redColor, Color blueColor)
    {
        beam = beamLine;
        guide = guideLine;
        red = redColor;
        blue = blueColor;
    }

    void Awake()
    {
        player = GetComponent<DemoPlayer>();
        fireAction = InputRef.Find("Player/Attack", this);
        cooldown = new CooldownTimer(cooldownSeconds);
        enabled = fireAction != null && beam != null && guide != null;
    }

    void Update()
    {
        cooldown.Tick(Time.deltaTime);

        bool ready = CanFire();
        Trace(out ShotResult result, out Vector3 end, out ColorTarget target);
        Color own = player.Color.Value == ColorId.Red ? red : blue;
        Color guideColor = ready ? own : blocked;
        guideColor.a = ready ? 0.45f : 0.15f;
        DrawLine(guide, player.Muzzle, end, guideColor);

        if (ready && fireAction.WasPressedThisFrame() && cooldown.TryUse())
        {
            DrawLine(beam, player.Muzzle, end, own);
            beamRemaining = beamSeconds;
            if (result.Exchanges && target != null) { ExchangeService.TryExchange(player.Color, target); }
        }

        beamRemaining -= Time.deltaTime;
        beam.enabled = beamRemaining > 0f;
    }

    private bool CanFire()
    {
        return player.Grounded && player.OnFlat && !InsideTarget();
    }

    // shots from inside a wall or source would skip it, so they are refused
    private bool InsideTarget()
    {
        Vector3 feet = transform.position;
        float r = player.Radius;
        int count = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * (r + 0.1f), feet + Vector3.up * (1.6f - r), r * 0.9f, overlaps, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            ColorTarget t = ColorContactPolicy.Owner(overlaps[i]);
            if (t != null && t.Kind != TargetKind.Platform) { return true; }
        }
        return false;
    }

    private void Trace(out ShotResult result, out Vector3 end, out ColorTarget target)
    {
        Vector3 origin = player.Muzzle;
        Vector3 direction = player.Facing;
        int count = Physics.RaycastNonAlloc(origin, direction, rayHits, range, ~0, QueryTriggerInteraction.Ignore);

        sorted.Clear();
        for (int i = 0; i < count; i++)
        {
            if (!IsMine(rayHits[i].collider)) { sorted.Add(rayHits[i]); }
        }
        sorted.Sort((a, b) => a.distance.CompareTo(b.distance));

        shotHits.Clear();
        shotTargets.Clear();
        foreach (RaycastHit hit in sorted)
        {
            ColorTarget t = ColorContactPolicy.Owner(hit.collider);
            shotTargets.Add(t);
            shotHits.Add(t == null ? new ShotHit(ShotHitKind.Normal) : new ShotHit(t.ShotKind, t.State.Value, t.Spent));
        }

        result = ShotRules.Resolve(player.Color.Value, shotHits);
        if (result.StopIndex < 0)
        {
            end = origin + direction * range;
            target = null;
            return;
        }
        end = sorted[result.StopIndex].point;
        target = shotTargets[result.StopIndex];
    }

    private bool IsMine(Collider c)
    {
        foreach (Collider mine in player.Bodies)
        {
            if (mine == c) { return true; }
        }
        return false;
    }

    private static void DrawLine(LineRenderer line, Vector3 from, Vector3 to, Color color)
    {
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startColor = color;
        line.endColor = color;
    }
}
