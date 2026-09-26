using System;
using UnityEngine;

// the authoritative red or blue value for the player or a target
public class ColorState : MonoBehaviour
{
    [SerializeField] private ColorId value;

    public ColorId Value => value;

    public event Action<ColorState> Changed;
    public static event Action<ColorState> AnyChanged;

    public void Configure(ColorId initial)
    {
        value = initial;
    }

    public void Set(ColorId next)
    {
        if (next == value) { return; }
        value = next;
        Changed?.Invoke(this);
        AnyChanged?.Invoke(this);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        AnyChanged = null;
    }
}
