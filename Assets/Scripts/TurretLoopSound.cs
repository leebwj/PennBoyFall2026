using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TurretLoopSound : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private AudioSource shootingAudio;

    void Awake()
    {
        shootingAudio = GetComponent<AudioSource>();
        shootingAudio.loop = true;
        shootingAudio.playOnAwake = false;
        shootingAudio.Stop();
    }

    void LateUpdate()
    {
        bool shooting = animator != null
                        && animator.GetBool("IsShooting");

        if (shooting && !shootingAudio.isPlaying)
        {
            shootingAudio.Play();
        }
        else if (!shooting && shootingAudio.isPlaying)
        {
            shootingAudio.Stop();
        }
    }

    void OnDisable()
    {
        if (shootingAudio != null)
        {
            shootingAudio.Stop();
        }
    }
}