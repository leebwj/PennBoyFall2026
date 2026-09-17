using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    // What camera follows. Initially set to player
    [SerializeField] private Transform target;

    // Camera offset length as it follows player
    [SerializeField] private Vector3 offset = new Vector3(-8f, 6.5f, -8f);

    // Time taken to catch up with player 
    [SerializeField] private float smoothTime = 0.35f;

    // Check surruonding environment of player
    [SerializeField] private float lookAhead = 1.5f;

    // How fast camera is moving 
    private Vector3 velocity;


    // Initialize camera on the scene
    void Start()
    {
        // Does nothing when there is no player to follow
        if (target == null) { 
            return; 
        }

        // Focus straight into the target
        transform.position = FollowPoint() + offset;

        // Point camera at the spot it follows via cursor
        transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
    }

    // Ease camera to new player position
    void LateUpdate()
    { 
        if (target == null) { 
            return;
        }

        // Glides camera from where it is to where the target is. 
        transform.position = Vector3.SmoothDamp(transform.position, FollowPoint() + offset, ref velocity, smoothTime);
        transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
    }

    // Actual spot camera follows (slightly nudged towards player's front to show where player is facing)
    private Vector3 FollowPoint()
    {
        return target.position + target.forward * lookAhead;
    }
}
