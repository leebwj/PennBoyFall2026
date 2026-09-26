using UnityEngine;

// held down while the box overlaps it, regardless of anyone's colour
public class Plate : MonoBehaviour
{
    [SerializeField] private Rigidbody box;
    [SerializeField] private float radius = 0.8f;
    [SerializeField] private Renderer indicator;
    [SerializeField] private Material idle;
    [SerializeField] private Material pressed;

    public bool IsOccupied
    {
        get
        {
            if (box == null) { return false; }
            Vector3 d = box.position - transform.position;
            return Mathf.Abs(d.y) < 1.5f && new Vector2(d.x, d.z).magnitude < radius;
        }
    }

    public void Configure(Rigidbody pushedBox, Renderer ring, Material idleMaterial, Material pressedMaterial)
    {
        box = pushedBox;
        indicator = ring;
        idle = idleMaterial;
        pressed = pressedMaterial;
    }

    void Update()
    {
        if (indicator != null) { indicator.sharedMaterial = IsOccupied ? pressed : idle; }
    }
}
