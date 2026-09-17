using UnityEngine;

// Automatically add Colorable as ColoredWall required Colorable
[RequireComponent(typeof(Colorable))]


public class ColoredWall : MonoBehaviour
{

    // The current wall's color state
    private Colorable colorable;

    // Wall's current solid state (solid/passable)
    private Collider wallCollider;

    // Player's color state
    private Colorable player;

    
    void Start() {

        // Cheack color state of the wall object
        colorable = GetComponent<Colorable>();

        // Check collider on this wall object
        wallCollider = GetComponent<Collider>();

        // Scan player and check its current color state
        player = GameObject.FindWithTag("Player").GetComponent<Colorable>();
        
    }

    void Update() {

        // When player is opposite color, wall is triggered to be passable
        wallCollider.isTrigger = player.CurrentColor != colorable.CurrentColor;
    }
}