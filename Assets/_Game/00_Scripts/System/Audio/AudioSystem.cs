using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Slafurry.Core.Abstract;

namespace Slafurry.System.Audio
{
    public static class Audio
    {
        public static void PlayMusic(string trackName, float fade = 0.5f)
            => AudioSystem.Music.PlayMusic(trackName, fade);

        public static void StopMusic(float fade = 0.5f)
            => AudioSystem.Music.StopMusic(fade);

        public static void PlaySFX2D(string category, string effect, bool loop = false)
            => AudioSystem.SFX.PlaySFX2D(category, effect, loop);

        public static void PlaySFX3D(string category, string effect, Vector3 pos, bool loop = false)
            => AudioSystem.SFX.PlaySFX3D(category, effect, pos, loop);

        public static void StopSFX()
            => AudioSystem.SFX.StopAllSFX();

        public static void StopSFX(string category)
            => AudioSystem.SFX.StopCategory(category);

        public static void StopSFX(string category, string effect)
            => AudioSystem.SFX.StopSFX(category, effect);
    }

    public class AudioSystem : GameSystem<AudioSystem>
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Sub Players")]
        [SerializeField] private MusicPlayer musicPlayer;
        [SerializeField] private SFXPlayer sfxPlayer;

        public static MusicPlayer Music => Instance.musicPlayer;
        public static SFXPlayer SFX => Instance.sfxPlayer;

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;

        private const string MasterKey = "MasterVolume";
        private const string MusicKey = "MusicVolume";
        private const string SFXKey = "SFXVolume";

        // ======================== GAME SYSTEM LIFECYCLE ========================

        public override IEnumerator Initialize()
        {
            LoadVolume();

            yield return new WaitForSecondsRealtime(0.1f);
        }

        public override void PostInitialize()
        {
            musicPlayer.Initialize();
            // Retrieve saved volume
            float masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            float musicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
            float sfxVolume = PlayerPrefs.GetFloat(SFXKey, 1f);

            // Apply volume to mixer
            UpdateMasterVolume(masterVolume);
            UpdateMusicVolume(musicVolume);
            UpdateSFXVolume(sfxVolume);

            PlaySceneMusic();
        }

        // ======================== VOLUME LOADER ========================

        public void LoadVolume()
        {
            float masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            float musicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
            float sfxVolume = PlayerPrefs.GetFloat(SFXKey, 1f);

            audioMixer.SetFloat(MasterKey, LinearToDecibel(masterVolume));
            audioMixer.SetFloat(MusicKey, LinearToDecibel(musicVolume));
            audioMixer.SetFloat(SFXKey, LinearToDecibel(sfxVolume));
        }

        // ======================== PUBLIC API ========================

        public void PlaySceneMusic()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            musicPlayer.PlayMusic(currentSceneName);
        }

        // ======================== VOLUME CHANGER ========================

        public void UpdateMasterVolume(float linearVolume)
        {
            linearVolume = Mathf.Clamp01(linearVolume);

            audioMixer.SetFloat(
                MasterKey,
                LinearToDecibel(linearVolume)
            );

            PlayerPrefs.SetFloat(MasterKey, linearVolume);
            PlayerPrefs.Save();

            OnMasterVolumeChanged?.Invoke(linearVolume);
        }

        public void UpdateMusicVolume(float linearVolume)
        {
            linearVolume = Mathf.Clamp01(linearVolume);

            audioMixer.SetFloat(
                MusicKey,
                LinearToDecibel(linearVolume)
            );

            PlayerPrefs.SetFloat(MusicKey, linearVolume);
            PlayerPrefs.Save();

            OnMusicVolumeChanged?.Invoke(linearVolume);
        }

        public void UpdateSFXVolume(float linearVolume)
        {
            linearVolume = Mathf.Clamp01(linearVolume);

            audioMixer.SetFloat(
                SFXKey,
                LinearToDecibel(linearVolume)
            );

            PlayerPrefs.SetFloat(SFXKey, linearVolume);
            PlayerPrefs.Save();

            OnSFXVolumeChanged?.Invoke(linearVolume);
        }

        // ======================== UTILITIES ========================

        private float LinearToDecibel(float linear)
        {
            return linear > 0.0001f
                ? Mathf.Log10(linear) * 20f
                : -80f;
        }

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
        }
    }
}