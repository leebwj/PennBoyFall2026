using System.Collections;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public enum Kind
    {
        Health,
        Score,
    }

    [SerializeField] private Kind kind = Kind.Health;
    [SerializeField] private float amount = 25f;
    [SerializeField] private float respawnSeconds = 6f;
    [SerializeField] private float spinDegreesPerSecond = 120f;
    [SerializeField] private float bobHeight = 0.15f;

    private Collider trigger;
    private Renderer[] visuals;
    private Vector3 restPosition;
    private bool available = true;

    void Awake()
    {
        trigger = GetComponent<Collider>();
        visuals = GetComponentsInChildren<Renderer>();
        restPosition = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
        float bob = Mathf.Sin(Time.time * 2f) * bobHeight;
        transform.position = restPosition + new Vector3(0f, bob, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!available || !other.CompareTag("Player")) { return; }

        if (kind == Kind.Health)
        {
            Damageable health = other.GetComponentInParent<Damageable>();
            if (health == null || health.Fraction >= 1f) { return; }
            health.Heal(amount);
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(Mathf.RoundToInt(amount));
        }

        StartCoroutine(Recharge());
    }

    private IEnumerator Recharge()
    {
        SetAvailable(false);
        yield return new WaitForSeconds(respawnSeconds);
        SetAvailable(true);
    }

    private void SetAvailable(bool state)
    {
        available = state;
        trigger.enabled = state;
        foreach (Renderer r in visuals) { r.enabled = state; }
    }
}
