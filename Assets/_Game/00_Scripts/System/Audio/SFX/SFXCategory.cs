using Slafurry.Utils.Attributes;
using UnityEngine;


[System.Serializable]
public class SFXEffect
{
    public string groupID;
    public AudioClip[] clips;
    [Range(0f, 10f)]
    public float volume = 1f;
    public int maxSimultaneous = 3;

    [Header("Fade")]
    [Tooltip("Durasi fade-in default (detik) saat efek ini diputar. 0 = langsung full volume.")]
    [Range(0f, 5f)]
    public float fadeIn = 0f;
    [Tooltip("Durasi fade-out default (detik) saat efek ini dihentikan. 0 = langsung berhenti.")]
    [Range(0f, 5f)]
    public float fadeOut = 0f;

    [Header("Jarak (dipakai di PlaySFX3D)")]
    [Tooltip("Kalau dicentang, suara cuma berubah VOLUME berdasarkan jarak, tanpa panning kiri/kanan. Cocok buat game 2D/top-down.")]
    public bool distanceOnlyNoPanning = true;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
    [Tooltip("Jarak di bawah ini volume full 100%.")]
    public float minDistance = 1f;
    [Tooltip("Jarak di atas ini suara sudah tidak terdengar sama sekali.")]
    public float maxDistance = 15f;
}

namespace Slafurry.System.Audio
{

    [GameAssetCreator("Audio/SFX", "SFX Category", order: 2)]
    public class SFXCategory : ScriptableObject
    {
        public string categoryName;
        public SFXEffect[] effects;
        public int poolSize = 8;
    }
}