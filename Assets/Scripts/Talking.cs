using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class LevelDialogueAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] groupA;
    [SerializeField] private AudioClip[] groupB;

    private AudioSource voice;

    IEnumerator Start()
    {
        voice = GetComponent<AudioSource>();
        voice.loop = false;
        voice.Stop();

        yield return PlayGroup(groupA);
        yield return PlayGroup(groupB);
    }

    IEnumerator PlayGroup(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            yield break;
        }

        AudioClip chosen = clips[Random.Range(0, clips.Length)];

        if (chosen == null)
        {
            yield break;
        }

        voice.clip = chosen;
        voice.Play();

        while (voice.isPlaying)
        {
            yield return null;
        }
    }
}