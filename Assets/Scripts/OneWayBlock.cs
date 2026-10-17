using UnityEngine;

// Player can only walk through one way
public class OneWayBlock : MonoBehaviour
{

    // Current solid/passable state
    private Collider blockCollider;

    // Player's position
    private Transform player;

    // True while the player is partway through block
    private bool playerInside;


    void Start() {

        blockCollider = GetComponent<Collider>();
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update() {

        // Below 0 when the player is behind the block
        float side = Vector3.Dot(player.position - transform.position, transform.forward);

        // Passable from behind, and stays passable until the player is all the way through
        blockCollider.isTrigger = (side < 0f) || (playerInside);
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            playerInside = true;
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            playerInside = false;
        }
    }
}
