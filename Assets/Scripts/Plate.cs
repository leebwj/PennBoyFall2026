using UnityEngine;

// Opens gate while the player or a box stands on top
public class Plate : MonoBehaviour
{

    // Specific gate this plate opens
    [SerializeField] private Gate gate;


    void Update() {

        gate.SetOpen(IsPressed());
    }

    // True when something stands on plate
    private bool IsPressed() {

        Collider[] hits = Physics.OverlapBox(transform.position + Vector3.up * 0.5f, new Vector3(0.5f, 0.5f, 0.5f), transform.rotation, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits) {
            Rigidbody body = hit.attachedRigidbody;

            if (body != null && !body.isKinematic) {
                return true;
            }
        }
        return false;
    }
}
