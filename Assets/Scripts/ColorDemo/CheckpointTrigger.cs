using UnityEngine;

// placed at the start of each incoming connector, before any target of the next encounter is in range
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private ColorRegion region;

    public void Configure(ColorRegion target)
    {
        region = target;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<DemoPlayer>() == null || DemoDirector.Instance == null) { return; }
        DemoDirector.Instance.Enter(region);
    }
}
