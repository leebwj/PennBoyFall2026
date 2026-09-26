using System.Collections;
using UnityEngine;

// swaps renderers between the red and blue materials and dims spent targets
public class ColorTint : MonoBehaviour
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    [SerializeField] private ColorState state;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Material red;
    [SerializeField] private Material blue;
    [SerializeField] private float spentDim = 0.4f;
    [SerializeField] private float pulseScale = 1.12f;
    [SerializeField] private float pulseSeconds = 0.22f;

    private MaterialPropertyBlock block;
    private ColorTarget target;
    private Vector3[] baseScales;

    public void Configure(ColorState colorState, Renderer[] tinted, Material redMaterial, Material blueMaterial)
    {
        state = colorState;
        renderers = tinted;
        red = redMaterial;
        blue = blueMaterial;
    }

    void Awake()
    {
        block = new MaterialPropertyBlock();
        target = GetComponent<ColorTarget>();
        baseScales = new Vector3[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) { baseScales[i] = renderers[i].transform.localScale; }
    }

    void OnEnable()
    {
        state.Changed += OnChanged;
        Refresh();
    }

    void OnDisable()
    {
        state.Changed -= OnChanged;
    }

    private void OnChanged(ColorState source)
    {
        Refresh();
        if (isActiveAndEnabled) { StartCoroutine(Pulse()); }
    }

    public void Refresh()
    {
        Material material = state.Value == ColorId.Red ? red : blue;
        bool spent = target != null && target.Spent;
        foreach (Renderer r in renderers)
        {
            r.sharedMaterial = material;
            if (spent)
            {
                block.SetColor(BaseColor, material.GetColor(BaseColor) * spentDim);
                r.SetPropertyBlock(block);
            }
            else
            {
                r.SetPropertyBlock(null);
            }
        }
    }

    private IEnumerator Pulse()
    {
        for (float t = 0f; t < pulseSeconds; t += Time.deltaTime)
        {
            float k = Mathf.Lerp(pulseScale, 1f, t / pulseSeconds);
            for (int i = 0; i < renderers.Length; i++) { renderers[i].transform.localScale = baseScales[i] * k; }
            yield return null;
        }
        for (int i = 0; i < renderers.Length; i++) { renderers[i].transform.localScale = baseScales[i]; }
    }
}
