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

        public void PlayMusic(string trackName, float fadeDuration = 0.5f)
        {
            if (musicData == null || musicSource == null) return;

            MusicTrack track = musicData.GetTrack(trackName);
            if (track.clip == null) return;

            StopAllPlayback();

            if (track.introClip != null)
                _currentFadeCoroutine = StartCoroutine(PlayIntroThenLoop(track, fadeDuration));
            else
                _currentFadeCoroutine = StartCoroutine(AnimateMusicCrossfade(track.clip, track.volume, false, fadeDuration));
        }

        public void StopMusic(float fadeDuration = 0.5f)
        {
            if (musicSource == null) return;

            StopAllPlayback();

            if (fadeDuration <= 0f)
            {
                musicSource.Stop();
                musicSource.clip = null;
                musicSource.volume = 0f;
                return;
            }

            _currentFadeCoroutine = StartCoroutine(FadeOutAndStop(fadeDuration));
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
            // Fade out lagu sebelumnya
            float startVolume = musicSource.volume;
            float percent = 0f;
            while (percent < 1f)
            {
                percent += Time.unscaledDeltaTime / fadeDuration;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, percent);
                yield return null;
            }

            // Putar intro (sekali, tidak loop)
            musicSource.clip = track.introClip;
            musicSource.loop = false;
            musicSource.Play();

            // Tunggu sampai intro selesai
            while (musicSource.isPlaying)
                yield return null;

            // Crossfade ke loop clip
            _currentFadeCoroutine = StartCoroutine(AnimateMusicCrossfade(track.clip, track.volume, true, fadeDuration));
        }

        // ======================== CROSSFADE ========================

        private IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float targetVolume, bool loop, float fadeDuration = 0.5f)
        {
            float startVolume = musicSource.volume;
            float percent = 0f;
            while (percent < 1f)
            {
                percent += Time.unscaledDeltaTime / fadeDuration;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, percent);
                yield return null;
            }

            musicSource.clip = nextTrack;
            musicSource.loop = loop;
            musicSource.Play();

            percent = 0f;
            while (percent < 1f)
            {
                percent += Time.unscaledDeltaTime / fadeDuration;
                musicSource.volume = Mathf.Lerp(0f, targetVolume, percent);
                yield return null;
            }

            musicSource.volume = targetVolume;
            _currentFadeCoroutine = null;
        }

        // ======================== FADE OUT & STOP ========================

        private IEnumerator FadeOutAndStop(float fadeDuration)
        {
            float startVolume = musicSource.volume;
            float percent = 0f;

            while (percent < 1f)
            {
                percent += Time.unscaledDeltaTime / fadeDuration;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, percent);
                yield return null;
            }

            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = startVolume;

            _currentFadeCoroutine = null;
        }
    }
}