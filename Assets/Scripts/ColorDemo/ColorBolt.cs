using System.Collections.Generic;
using UnityEngine;

// an enemy projectile. normal geometry stops it, coloured bodies let it through,
// and it only harms a player of its own launch colour
public class ColorBolt : MonoBehaviour
{
    private const float Radius = 0.15f;

    public static readonly List<ColorBolt> Live = new List<ColorBolt>();

    private readonly RaycastHit[] hits = new RaycastHit[16];

    private ColorId color;
    private Vector3 direction;
    private float speed;
    private float remaining;
    private Transform owner;

    public static ColorBolt Launch(Vector3 origin, Vector3 direction, ColorId color, Material material, float speed, float range, Transform owner)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Bolt";
        Destroy(body.GetComponent<Collider>());
        body.transform.position = origin;
        body.transform.localScale = Vector3.one * (Radius * 2.4f);
        body.GetComponent<MeshRenderer>().sharedMaterial = material;

        ColorBolt bolt = body.AddComponent<ColorBolt>();
        bolt.color = color;
        bolt.direction = direction;
        bolt.speed = speed;
        bolt.remaining = range;
        bolt.owner = owner;
        return bolt;
    }

    public static void ClearAll()
    {
        for (int i = Live.Count - 1; i >= 0; i--) { Destroy(Live[i].gameObject); }
        Live.Clear();
    }

    void OnEnable()
    {
        Live.Add(this);
    }

    void OnDisable()
    {
        Live.Remove(this);
    }

    void Update()
    {
        float step = Mathf.Min(speed * Time.deltaTime, remaining);
        int count = Physics.SphereCastNonAlloc(transform.position, Radius, direction, hits, step, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, 0, count, HitComparer.Instance);

        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i].collider;
            if (owner != null && c.transform.IsChildOf(owner)) { continue; }

            DemoPlayer player = c.GetComponentInParent<DemoPlayer>();
            if (player != null)
            {
                if (player.Color.Value != color) { continue; }
                Destroy(gameObject);
                DemoDirector.Instance.Retry();
                return;
            }

            if (ColorContactPolicy.Owner(c) != null) { continue; }
            Destroy(gameObject);
            return;
        }

        transform.position += direction * step;
        remaining -= step;
        if (remaining <= 0f) { Destroy(gameObject); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Live.Clear();
    }

    private class HitComparer : IComparer<RaycastHit>
    {
        public static readonly HitComparer Instance = new HitComparer();

        public int Compare(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }
    }
}
