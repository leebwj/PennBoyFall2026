using System.Collections;
using UnityEngine;

// a stationary turret. every volley keeps the colour it was launched with, and only
// hurts a player of that same colour. spent enemies keep firing but cannot exchange again
[RequireComponent(typeof(ColorTarget))]
public class ColorEnemy : MonoBehaviour
{
    [SerializeField] private bool attacks = true;
    [SerializeField] private float intervalSeconds = 4f;
    [SerializeField] private float warningSeconds = 0.8f;
    [SerializeField] private float volleySeconds = 0.6f;
    [SerializeField] private int shotsPerVolley = 3;
    [SerializeField] private float boltSpeed = 6f;
    [SerializeField] private float range = 12f;
    [SerializeField] private float muzzleHeight = 1f;
    [SerializeField] private Transform visual;
    [SerializeField] private Material red;
    [SerializeField] private Material blue;

    private ColorTarget target;

    public void Configure(bool canAttack, Transform visualRoot, Material redMaterial, Material blueMaterial)
    {
        attacks = canAttack;
        visual = visualRoot;
        red = redMaterial;
        blue = blueMaterial;
    }

    void Awake()
    {
        target = GetComponent<ColorTarget>();
    }

    void OnEnable()
    {
        if (attacks) { StartCoroutine(Cycle()); }
    }

    void Update()
    {
        if (visual == null) { return; }
        float spin = target.Spent ? 8f : 40f;
        visual.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
    }

    private IEnumerator Cycle()
    {
        var rest = new WaitForSeconds(intervalSeconds - warningSeconds - volleySeconds);
        var gap = new WaitForSeconds(volleySeconds / Mathf.Max(1, shotsPerVolley));
        while (true)
        {
            yield return rest;
            if (!PlayerInReach()) { continue; }

            // the warning is a swell of the body, no text or sound needed
            Vector3 baseScale = visual.localScale;
            for (float t = 0f; t < warningSeconds; t += Time.deltaTime)
            {
                visual.localScale = baseScale * (1f + 0.18f * Mathf.Sin(t / warningSeconds * Mathf.PI));
                yield return null;
            }
            visual.localScale = baseScale;

            for (int i = 0; i < shotsPerVolley; i++)
            {
                Fire();
                yield return gap;
            }
        }
    }

    private bool PlayerInReach()
    {
        DemoDirector director = DemoDirector.Instance;
        if (director == null || director.Player == null) { return false; }
        Vector3 d = director.Player.transform.position - transform.position;
        return Mathf.Abs(d.y) < 2f && new Vector2(d.x, d.z).magnitude <= range;
    }

    private void Fire()
    {
        DemoPlayer player = DemoDirector.Instance.Player;
        Vector3 origin = transform.position + Vector3.up * muzzleHeight;
        Vector3 aim = player.transform.position - transform.position;
        aim.y = 0f;
        if (aim.sqrMagnitude < 0.01f) { return; }

        ColorId color = target.State.Value;
        ColorBolt.Launch(origin, aim.normalized, color, color == ColorId.Red ? red : blue, boltSpeed, range, transform);
    }
}
