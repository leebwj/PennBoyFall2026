using UnityEngine;

public class Target : MonoBehaviour
{
    public void Hit()
    {
        Destroy(gameObject);
    }
}
