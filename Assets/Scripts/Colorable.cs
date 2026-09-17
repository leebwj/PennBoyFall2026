using UnityEngine;

public class Colorable : MonoBehaviour
{
    // Current object color. Initially set to Red (Change in inspector)
    [SerializeField] private GameColor color = GameColor.Red; 

    // Read only to know what color the object currently is set to.
    public GameColor CurrentColor => color;


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

        // If color is set to Red, apply red color. Otherwise, apply blue
        Color tint = color == GameColor.Red ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.25f, 0.45f, 0.9f);

        foreach (Renderer r in GetComponentsInChildren<Renderer>()) {

            // Change material color to the chosen tint color
            r.material.color = tint;

        }


    }


}
