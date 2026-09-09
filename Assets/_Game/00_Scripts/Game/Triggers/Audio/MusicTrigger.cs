using Slafurry.System.Audio;
using UnityEngine;

public class MusicTrigger : BaseTrigger
{
    [Header("Music Track")]
    [SerializeField] private string trackName;
    [SerializeField] private float fadeDuration = -1f;

    public void PlayMusic()
    {
        if (!CanTrigger()) return;
        Audio.PlayMusic(trackName, fadeDuration);
        AddTriggerCount();
    }

    public void StopMusic(float fadeOverride)
    {
        if (!CanTrigger()) return;
        Audio.StopMusic(fadeOverride > 0f ? fadeOverride : fadeDuration);
        AddTriggerCount();
    }
}