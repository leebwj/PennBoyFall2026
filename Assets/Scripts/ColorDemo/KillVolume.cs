using UnityEngine;

// a pit fall retries the active encounter
public class KillVolume : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<DemoPlayer>() == null || DemoDirector.Instance == null) { return; }
        DemoDirector.Instance.Retry();
    }
}
