using UnityEngine;

// Move gate down when open, up back when closed
public class Gate : MonoBehaviour
{

    // How far the gate sinks
    [SerializeField] private float drop = 2.1f;

    // Gate movement speed
    [SerializeField] private float speed = 4f;

    // Set by a plate
    private bool open;

    // Where the gate sits when closed
    private Vector3 closedPosition;


    void Start() {
        closedPosition = transform.position;
    }

    void Update() {

        // Move to open or closed position each frame
        Vector3 target = open ? closedPosition + Vector3.down * drop : closedPosition;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    // Called by a plate to open or close this gate
    public void SetOpen(bool value) {

        open = value;
    }
}
