using UnityEngine;

[System.Serializable]
public struct SceneTrack
{
    public string sceneName;
    public string trackName;
}

[System.Serializable]
public struct MusicTrack
{
    public string trackName;
    public AudioClip clip;
    [Tooltip("Clip intro yang diputar sekali sebelum clip utama (loop). Kosongkan kalau tidak ada intro.")]
    public AudioClip introClip;
    [Range(0f, 10f)]
    public float volume;
}
