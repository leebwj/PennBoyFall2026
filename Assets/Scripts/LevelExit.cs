using UnityEngine;

// Green pad object is the goal. 
public class LevelExit : MonoBehaviour
{

    // Run when "player" steps on the pad
    void OnTriggerEnter(Collider other) {

        // Ignore non-player objects
        if (!other.CompareTag("Player")) {
            return;
        }

        // Otherwise level complete
        Debug.Log("Level Complete!");
    }
}
