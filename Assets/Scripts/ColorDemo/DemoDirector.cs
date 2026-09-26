using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// tracks the active checkpoint region and performs retries
public class DemoDirector : MonoBehaviour
{
    public static DemoDirector Instance { get; private set; }

    [SerializeField] private DemoPlayer player;
    [SerializeField] private ColorRegion firstRegion;

    [Header("Testing")]
    [Tooltip("Route number (1-15) to start at when Play is pressed. 0 starts from the beginning.")]
    [SerializeField] private int startRoute;
    [Tooltip("] and [ jump to the next or previous encounter while playing.")]
    [SerializeField] private bool jumpKeys = true;

    private ColorRegion[] regions;

    public DemoPlayer Player => player;
    public ColorRegion Active { get; private set; }

    public void Configure(DemoPlayer demoPlayer, ColorRegion first)
    {
        player = demoPlayer;
        firstRegion = first;
    }

    void Awake()
    {
        Instance = this;
        regions = FindObjectsByType<ColorRegion>(FindObjectsSortMode.None).OrderBy(r => r.Route).ToArray();
    }

    void Start()
    {
        ColorRegion start = FindRoute(startRoute);
        if (start != null && start != firstRegion) { JumpTo(start); }
        else if (firstRegion != null) { Enter(firstRegion); }
    }

    void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
    }

    void Update()
    {
        Keyboard keys = Keyboard.current;

        // manual restart of the current encounter when the player gets stuck
        bool key = keys != null && keys.rKey.wasPressedThisFrame;
        bool pad = Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame;
        if (key || pad) { Retry(); }

        if (jumpKeys && keys != null && Active != null)
        {
            if (keys.rightBracketKey.wasPressedThisFrame) { Step(1); }
            if (keys.leftBracketKey.wasPressedThisFrame) { Step(-1); }
        }
    }

    public void Enter(ColorRegion region)
    {
        if (region == null) { return; }
        region.Capture(player.Color.Value);
        Active = region;
    }

    public void Retry()
    {
        if (Active == null) { return; }
        ColorBolt.ClearAll();
        Active.Restore(player);
    }

    // places the player at an encounter's checkpoint with the colour the design expects on arrival.
    // an encounter visited before is restored to its first-entry snapshot instead
    public void JumpTo(ColorRegion region)
    {
        ColorBolt.ClearAll();
        if (!region.Captured) { player.Teleport(region.Respawn, region.Arrival); }
        Enter(region);
        region.Restore(player);
    }

    private void Step(int direction)
    {
        int index = System.Array.IndexOf(regions, Active) + direction;
        if (index >= 0 && index < regions.Length) { JumpTo(regions[index]); }
    }

    private ColorRegion FindRoute(int route)
    {
        return regions.FirstOrDefault(r => r.Route == route);
    }
}
