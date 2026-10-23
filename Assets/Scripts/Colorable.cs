using UnityEngine;

public class Colorable : MonoBehaviour
{
    // Current object color. Initially set to Red (Change in inspector)
    [SerializeField] private GameColor color = GameColor.Red; 

    // Material slots that show the color
    [SerializeField] private int[] tintSlots = new int[0];

    // Read only to know what color the object currently is set to.
    public GameColor CurrentColor => color;

    // Red and blue in one place so objects / bullets match
    public static Color Tint(GameColor c) {
        return c == GameColor.Red ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.25f, 0.45f, 0.9f);
    }

    // Unity calls once when game starts.
    void Start() {

        ApplyTint(); 

    }

    // Change object color
    public void SetColor(GameColor next) {
        
        color = next;  
        ApplyTint();

    }

    // Color all visible part to match its set color
    private void ApplyTint() {

        Color tint = Tint(color);

        foreach (Renderer r in GetComponentsInChildren<Renderer>()) {

            // A model has one material per part. materials gives this object its own copies to recolor
            Material[] mats = r.materials;

            for (int i = 0; i < mats.Length; i++) {
                if (tintSlots.Length == 0 || System.Array.IndexOf(tintSlots, i) >= 0) {
                    mats[i].SetColor("_Colour", tint);
                }
            }
        }
    }
}
