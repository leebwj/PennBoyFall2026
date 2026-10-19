using UnityEngine;
using UnityEngine.SceneManagement;

// Reach green pad to clear level 
public class LevelExit : MonoBehaviour
{

    // When player steps on the green exit pad
    void OnTriggerEnter(Collider other) {

        // Ignore non-player objects
        if (!other.CompareTag("Player")) {
            return;
        }

        // Otherwise level complete
        Debug.Log("Level Complete!");

        // Next level in Build Settings order
        int next = SceneManager.GetActiveScene().buildIndex + 1;

        // If on last level, nothing happens
        if (next >= SceneManager.sceneCountInBuildSettings) {
            return;
        }
        
        // Otherwise load next scene (File -> Build Profiles -> Scene List for editing level order)
        SceneManager.LoadScene(next);
    }
}
