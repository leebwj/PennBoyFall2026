using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerLife : MonoBehaviour
{
    [SerializeField] private float respawnDelaySeconds = 1.2f;
    [SerializeField] private GameObject deathEffect;

    private Damageable damageable;
    private Rigidbody rb;
    private PlayerController controller;
    private PlayerShooter shooter;
    private Renderer[] visuals;

    void Awake()
    {
        damageable = GetComponent<Damageable>();
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<PlayerController>();
        shooter = GetComponent<PlayerShooter>();
        visuals = GetComponentsInChildren<Renderer>();
    }

    void OnEnable()
    {
        damageable.Died += OnDied;
    }

    void OnDisable()
    {
        damageable.Died -= OnDied;
    }

    private void OnDied(Damageable source)
    {
        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        if (deathEffect != null) { Instantiate(deathEffect, transform.position, Quaternion.identity); }
        if (GameManager.Instance != null) { GameManager.Instance.ReportDeath(); }

        SetActiveState(false);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        yield return new WaitForSeconds(respawnDelaySeconds);

        Vector3 point = GameManager.Instance != null ? GameManager.Instance.RespawnPoint : Vector3.up;
        rb.position = point;
        transform.position = point;
        damageable.Revive();
        SetActiveState(true);
    }

    private void SetActiveState(bool active)
    {
        if (controller != null) { controller.enabled = active; }
        if (shooter != null) { shooter.enabled = active; }
        foreach (Renderer r in visuals) { r.enabled = active; }
    }
}
