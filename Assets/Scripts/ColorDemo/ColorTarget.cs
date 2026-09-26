using System.Collections.Generic;
using UnityEngine;

public enum TargetKind { Wall, Source, Enemy, Box, Lever, Platform }

// anything with a colour the player can exchange with or phase through
[RequireComponent(typeof(ColorState))]
public class ColorTarget : MonoBehaviour
{
    [SerializeField] private string id;
    [SerializeField] private TargetKind kind;
    [SerializeField] private bool spent;

    public static readonly List<ColorTarget> All = new List<ColorTarget>();

    public string Id => id;
    public TargetKind Kind => kind;
    public bool Spent => spent;
    public ColorState State { get; private set; }
    public Collider[] Bodies { get; private set; }
    public ShotHitKind ShotKind => kind == TargetKind.Wall ? ShotHitKind.Wall : ShotHitKind.Target;

    public void Configure(string targetId, TargetKind targetKind)
    {
        id = targetId;
        kind = targetKind;
    }

    void Awake()
    {
        State = GetComponent<ColorState>();
        var bodies = new List<Collider>();
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (!c.isTrigger) { bodies.Add(c); }
        }
        Bodies = bodies.ToArray();
    }

    void OnEnable()
    {
        All.Add(this);
        ColorContactPolicy.Register(this);
    }

    void OnDisable()
    {
        All.Remove(this);
        ColorContactPolicy.Unregister(this);
    }

    public void SetSpent(bool value)
    {
        spent = value;
        ColorTint tint = GetComponent<ColorTint>();
        if (tint != null) { tint.Refresh(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        All.Clear();
    }
}
