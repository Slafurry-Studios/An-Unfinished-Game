using UnityEngine;
using System.Collections;
using Slafurry.System.Scene;

namespace Slafurry.System.Audio
{
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private MusicData musicData;
        [SerializeField] private AudioSource musicSource;

        [Header("Scene Music")]
        [SerializeField] private SceneTrack[] sceneTracks;

        private Coroutine _currentFadeCoroutine;
        private Coroutine _introCoroutine;
        private MusicTrack _currentTrack;
        private bool _hasTrack;

        public void Initialize()
        {
            SceneLoader.Instance.OnSceneLoadCompleted += HandleSceneLoaded;
        }

        void OnDisable()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.OnSceneLoadCompleted -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(string sceneName)
        {
            string trackToPlay = null;

            if (sceneTracks != null)
            {
                foreach (var st in sceneTracks)
                {
                    if (!string.IsNullOrEmpty(st.sceneName) && st.sceneName == sceneName)
                    {
                        trackToPlay = st.trackName;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(trackToPlay))
                trackToPlay = sceneName;

            if (musicData != null && musicData.GetClipFromName(trackToPlay) != null)
                PlayMusic(trackToPlay);
        }

        public void PlayMusic(string trackName, float fadeDuration = -1f)
        {
            if (musicData == null || musicSource == null) return;

            MusicTrack track = musicData.GetTrack(trackName);
            if (track.clip == null) return;

            StopAllPlayback();

            _currentTrack = track;
            _hasTrack = true;

            float fadeIn = fadeDuration > 0f ? fadeDuration : track.FadeIn;

            if (track.introClip != null)
                _currentFadeCoroutine = StartCoroutine(PlayIntroThenLoop(track, fadeIn));
            else
                _currentFadeCoroutine = StartCoroutine(AnimateMusicCrossfade(track.clip, track.volume, false, fadeIn));
        }

        public void StopMusic(float fadeDuration = -1f)
        {
            if (musicSource == null) return;

            StopAllPlayback();

            float fadeOut = fadeDuration > 0f ? fadeDuration : (_hasTrack ? _currentTrack.FadeOut : 1f);

            if (fadeOut <= 0f)
            {
                musicSource.Stop();
                musicSource.clip = null;
                musicSource.volume = 0f;
                _hasTrack = false;
                return;
            }

            _currentFadeCoroutine = StartCoroutine(FadeOutAndStop(fadeOut));
        }

        private void StopAllPlayback()
        {
            if (_currentFadeCoroutine != null)
            {
                StopCoroutine(_currentFadeCoroutine);
                _currentFadeCoroutine = null;
            }

            if (_introCoroutine != null)
            {
                StopCoroutine(_introCoroutine);
                _introCoroutine = null;
            }
        }

        // ======================== INTRO + LOOP ========================

        private IEnumerator PlayIntroThenLoop(MusicTrack track, float fadeDuration)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            musicSource.clip = track.introClip;
            musicSource.loop = false;
            musicSource.Play();

            while (musicSource.isPlaying)
                yield return null;

            _currentFadeCoroutine = StartCoroutine(AnimateMusicCrossfade(track.clip, track.volume, true, fadeDuration));
        }

        // ======================== CROSSFADE ========================

        private IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float targetVolume, bool loop, float fadeDuration = 0.5f)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            musicSource.clip = nextTrack;
            musicSource.loop = loop;
            musicSource.Play();

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                musicSource.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            musicSource.volume = targetVolume;
            _currentFadeCoroutine = null;
        }

        // ======================== FADE OUT & STOP ========================

        private IEnumerator FadeOutAndStop(float fadeDuration)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = startVolume;

            _hasTrack = false;
            _currentFadeCoroutine = null;
        }
    }
}