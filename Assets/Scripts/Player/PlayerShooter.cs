using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float shotsPerSecond = 5f;
    [SerializeField] private float damagePerShot = 25f;
    [SerializeField] private float cameraShake = 0.08f;

    private InputAction fireAction;
    private CooldownTimer cooldown;
    private CameraRig rig;

    void Awake()
    {
        fireAction = InputRef.Find("Player/Attack", this);
        cooldown = new CooldownTimer(1f / Mathf.Max(0.1f, shotsPerSecond));
        rig = FindFirstObjectByType<CameraRig>();
        enabled = fireAction != null && projectilePrefab != null && muzzle != null;
    }

    void Update()
    {
        cooldown.Tick(Time.deltaTime);
        if (!fireAction.IsPressed() || !cooldown.TryUse()) { return; }

        Quaternion rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
        Projectile shot = Instantiate(projectilePrefab, muzzle.position, rotation);
        shot.Launch(gameObject, damagePerShot);

        if (rig != null) { rig.Shake(cameraShake, 0.08f); }
    }
}
