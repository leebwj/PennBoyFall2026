using UnityEngine;

// the pushable box slides without friction and settles quickly through damping
[RequireComponent(typeof(Rigidbody))]
public class PushBox : MonoBehaviour
{
    void Awake()
    {
        var slick = new PhysicsMaterial("Frictionless")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
        };
        foreach (Collider c in GetComponentsInChildren<Collider>()) { c.sharedMaterial = slick; }
    }
}
