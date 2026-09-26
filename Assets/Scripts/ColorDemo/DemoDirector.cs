using UnityEngine;
using UnityEngine.InputSystem;

// tracks the active checkpoint region and performs retries
public class DemoDirector : MonoBehaviour
{
    public static DemoDirector Instance { get; private set; }

    [SerializeField] private DemoPlayer player;
    [SerializeField] private ColorRegion firstRegion;

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
    }

    void Start()
    {
        if (firstRegion != null) { Enter(firstRegion); }
    }

    void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
    }

    void Update()
    {
        // manual restart of the current encounter when the player gets stuck
        bool key = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        bool pad = Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame;
        if (key || pad) { Retry(); }
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
}
