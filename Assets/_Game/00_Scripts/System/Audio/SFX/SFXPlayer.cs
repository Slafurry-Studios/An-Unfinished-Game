using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Slafurry.System.Audio
{
    public class SFXPlayer : MonoBehaviour
    {
        [SerializeField] private SFXData sfxData;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;

        private class CategoryPool
        {
            public AudioSource[] Sources;
            public int OldestIndex;
            public readonly Dictionary<AudioSource, string> Playing = new();
            public readonly Dictionary<AudioSource, Coroutine> FadeCoroutines = new();
            public readonly Dictionary<AudioSource, int> PlayTokens = new();
        }

        private readonly Dictionary<string, CategoryPool> _pools = new();

        void Awake()
        {
            InitPools();
        }

        private void InitPools()
        {
            if (sfxData?.categories == null) return;

            foreach (var category in sfxData.categories)
            {
                var pool = new CategoryPool
                {
                    Sources = new AudioSource[category.poolSize]
                };

                for (int i = 0; i < category.poolSize; i++)
                {
                    GameObject go = new GameObject($"SFX_{category.categoryName}_{i}");
                    go.transform.SetParent(transform);

                    AudioSource source = go.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.outputAudioMixerGroup = sfxMixerGroup;

                    pool.Sources[i] = source;
                }

                _pools[category.categoryName] = pool;
            }
        }

        private void StopFadeIfAny(CategoryPool pool, AudioSource source)
        {
            if (pool.FadeCoroutines.TryGetValue(source, out var co) && co != null)
            {
                StopCoroutine(co);
            }
            pool.FadeCoroutines.Remove(source);
        }

        private AudioSource GetAvailableSource(CategoryPool pool)
        {
            // 1) cari source yang beneran nganggur
            foreach (var source in pool.Sources)
            {
                if (!source.isPlaying)
                {
                    StopFadeIfAny(pool, source);
                    pool.Playing.Remove(source);
                    source.loop = false;
                    return source;
                }
            }

            // 2) semua sibuk -> utamakan curi source yang TIDAK sedang fade,
            //    biar fade-out yang lagi berjalan nggak kepotong paksa di tengah.
            //    Source yang sedang fade cuma dicuri kalau memang kepepet (langkah 3).
            AudioSource nonFadingCandidate = null;
            foreach (var source in pool.Sources)
            {
                if (!pool.FadeCoroutines.ContainsKey(source))
                {
                    nonFadingCandidate = source;
                    break;
                }
            }

            // 3) fallback: semua source lagi fade juga -> terpaksa pakai round-robin lama
            AudioSource stolen = nonFadingCandidate != null ? nonFadingCandidate : pool.Sources[pool.OldestIndex];

            StopFadeIfAny(pool, stolen);
            stolen.Stop();
            stolen.loop = false;
            pool.Playing.Remove(stolen);

            pool.OldestIndex = (pool.OldestIndex + 1) % pool.Sources.Length;

            return stolen;
        }

        private int CountPlaying(CategoryPool pool, string effectName)
        {
            int count = 0;
            foreach (var source in pool.Sources)
            {
                if (!source.isPlaying)
                {
                    pool.Playing.Remove(source);
                    continue;
                }

                if (pool.Playing.TryGetValue(source, out string currentName) && currentName == effectName)
                    count++;
            }
            return count;
        }

        // ========================= FADE CORE =========================
        // Interpolasi dilakukan di ranah desibel (dB), bukan linear.
        // Ini penting karena SFXEffect.volume bisa > 1 (boost/gain, bisa sampai clipping) —
        // fade linear/kuadratik di ranah amplitude akan "mentok" begitu sinyal clipping,
        // jadi kerasa berhenti di tengah jalan. Fade di ranah dB tetap kerasa mulus
        // berapa pun besar target volume-nya.
        private const float MinDecibel = -80f;

        private static float LinearToDecibel(float linear)
        {
            return linear > 0.0001f ? Mathf.Log10(linear) * 20f : MinDecibel;
        }

        private static float DecibelToLinear(float db)
        {
            return Mathf.Pow(10f, db / 20f);
        }

        private IEnumerator FadeRoutine(CategoryPool pool, AudioSource source, float targetVolume, float duration, bool stopWhenDone)
        {
            float startDb = LinearToDecibel(source.volume);
            float targetDb = LinearToDecibel(targetVolume);
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float linear = Mathf.Clamp01(t / duration);
                float db = Mathf.Lerp(startDb, targetDb, linear);
                source.volume = DecibelToLinear(db);
                yield return null;
            }

            source.volume = targetVolume;

            if (stopWhenDone)
            {
                source.Stop();
                pool.Playing.Remove(source);
            }

            pool.FadeCoroutines.Remove(source);
        }

        // ========================= 3D =========================
        // fadeIn < 0  => pakai default dari SFXEffect.fadeIn
        // Volume otomatis mengikuti jarak ke AudioListener berdasarkan
        // minDistance/maxDistance/rolloffMode di SFXEffect. Kalau
        // effect.distanceOnlyNoPanning true, suara tidak akan panning
        // kiri/kanan (spread 360°) — pas buat game 2D/top-down yang cuma
        // butuh "makin jauh makin pelan", bukan efek 3D penuh.
        public void PlaySFX3D(string categoryName, string effectName, Vector3 pos, bool loop = false, float fadeIn = -1f)
        {
            if (!TryPrepare(categoryName, effectName, out var pool, out var effect, out var clip))
                return;

            AudioSource source = GetAvailableSource(pool);
            source.transform.position = pos;
            source.spatialBlend = 1f;
            source.loop = loop;
            source.clip = clip;
            source.rolloffMode = effect.rolloffMode;
            source.minDistance = effect.minDistance;
            source.maxDistance = effect.maxDistance;
            source.spread = effect.distanceOnlyNoPanning ? 360f : 0f;
            pool.Playing[source] = effectName;

            float resolvedFadeIn = fadeIn < 0f ? effect.fadeIn : fadeIn;
            StartPlaybackWithFade(pool, source, effect.volume, resolvedFadeIn);
            ScheduleAutoTailFade(pool, source, clip, effect.fadeOut);
        }

        // ========================= 2D =========================
        // fadeIn < 0  => pakai default dari SFXEffect.fadeIn
        public void PlaySFX2D(string categoryName, string effectName, bool loop = false, float fadeIn = -1f)
        {
            if (!TryPrepare(categoryName, effectName, out var pool, out var effect, out var clip))
                return;

            AudioSource source = GetAvailableSource(pool);
            source.transform.position = Vector3.zero;
            source.spatialBlend = 0f;
            source.loop = loop;
            source.clip = clip;
            pool.Playing[source] = effectName;

            float resolvedFadeIn = fadeIn < 0f ? effect.fadeIn : fadeIn;
            StartPlaybackWithFade(pool, source, effect.volume, resolvedFadeIn);
            ScheduleAutoTailFade(pool, source, clip, effect.fadeOut);
        }

        private void StartPlaybackWithFade(CategoryPool pool, AudioSource source, float targetVolume, float fadeIn)
        {
            // Setiap kali source ini mulai play baru, invalidasi auto tail-fade
            // dari play sebelumnya (kalau ada) supaya nggak salah nge-fade audio yang baru.
            pool.PlayTokens[source] = pool.PlayTokens.TryGetValue(source, out var v) ? v + 1 : 1;

            if (fadeIn > 0f)
            {
                source.volume = 0f;
                source.Play();
                pool.FadeCoroutines[source] = StartCoroutine(FadeRoutine(pool, source, targetVolume, fadeIn, stopWhenDone: false));
            }
            else
            {
                source.volume = targetVolume;
                source.Play();
            }
        }

        // Jadwalkan fade-out otomatis di ekor clip (nggak perlu ada yang manggil Stop manual).
        // Ini yang benerin klik di akhir clip yang selama ini kebiarin main tanpa fade
        // karena caller-nya lupa/nggak pernah manggil StopSFX.
        private void ScheduleAutoTailFade(CategoryPool pool, AudioSource source, AudioClip clip, float fadeOutDuration)
        {
            if (fadeOutDuration <= 0f || clip == null || source.loop)
                return;

            int token = pool.PlayTokens.TryGetValue(source, out var v) ? v : 0;

            float pitch = Mathf.Max(Mathf.Abs(source.pitch), 0.01f);
            float delay = Mathf.Max((clip.length / pitch) - fadeOutDuration, 0f);

            StartCoroutine(AutoTailFadeRoutine(pool, source, token, delay, fadeOutDuration));
        }

        private IEnumerator AutoTailFadeRoutine(CategoryPool pool, AudioSource source, int token, float delay, float fadeOutDuration)
        {
            yield return new WaitForSeconds(delay);

            // Kalau source udah dipakai ulang buat play lain sebelum waktunya, batalkan.
            if (!pool.PlayTokens.TryGetValue(source, out var currentToken) || currentToken != token)
                yield break;

            if (source.isPlaying)
                StopSourceWithFade(pool, source, fadeOutDuration);
        }

        // ========================= STOP =========================
        // fadeOut < 0  => pakai default per-effect dari SFXEffect.fadeOut
        public void StopAllSFX(float fadeOut = -1f)
        {
            foreach (var kvp in _pools)
            {
                string categoryName = kvp.Key;
                CategoryPool pool = kvp.Value;

                foreach (var source in pool.Sources)
                {
                    if (!source.isPlaying)
                        continue;

                    float resolvedFadeOut = ResolveFadeOut(categoryName, pool, source, fadeOut);
                    StopSourceWithFade(pool, source, resolvedFadeOut);
                }
            }
        }

        public void StopCategory(string categoryName, float fadeOut = -1f)
        {
            if (!_pools.TryGetValue(categoryName, out var pool))
                return;

            foreach (var source in pool.Sources)
            {
                if (!source.isPlaying)
                    continue;

                float resolvedFadeOut = ResolveFadeOut(categoryName, pool, source, fadeOut);
                StopSourceWithFade(pool, source, resolvedFadeOut);
            }
        }

        public void StopSFX(string categoryName, string effectName, float fadeOut = -1f)
        {
            if (!_pools.TryGetValue(categoryName, out var pool))
                return;

            float resolvedFadeOut = fadeOut;
            if (resolvedFadeOut < 0f)
            {
                var effect = sfxData.GetSFXEffect(categoryName, effectName);
                resolvedFadeOut = effect?.fadeOut ?? 0f;
            }

            foreach (var source in pool.Sources)
            {
                if (!source.isPlaying)
                    continue;

                if (pool.Playing.TryGetValue(source, out string currentName) &&
                    currentName == effectName)
                {
                    StopSourceWithFade(pool, source, resolvedFadeOut);
                }
            }
        }

        private float ResolveFadeOut(string categoryName, CategoryPool pool, AudioSource source, float fadeOut)
        {
            if (fadeOut >= 0f)
                return fadeOut;

            if (pool.Playing.TryGetValue(source, out string effectName))
            {
                var effect = sfxData.GetSFXEffect(categoryName, effectName);
                if (effect != null)
                    return effect.fadeOut;
            }

            return 0f;
        }

        private void StopSourceWithFade(CategoryPool pool, AudioSource source, float fadeOut)
        {
            if (!source.isPlaying)
                return;

            StopFadeIfAny(pool, source);

            // Kalau clip-nya nggak loop, jangan minta fade lebih panjang dari sisa clip —
            // kalau dipaksa, audio-nya keburu abis duluan sebelum fade sempat turun ke 0,
            // hasilnya kedengeran kepotong mendadak, bukan fade.
            if (fadeOut > 0f && !source.loop && source.clip != null)
            {
                float pitch = Mathf.Max(Mathf.Abs(source.pitch), 0.01f);
                float remaining = Mathf.Max((source.clip.length - source.time) / pitch, 0f);
                fadeOut = Mathf.Min(fadeOut, remaining);
            }

            if (fadeOut > 0f)
            {
                pool.FadeCoroutines[source] = StartCoroutine(FadeRoutine(pool, source, 0f, fadeOut, stopWhenDone: true));
            }
            else
            {
                source.Stop();
                pool.Playing.Remove(source);
            }
        }

        // ========================= Shared lookup =========================
        private bool TryPrepare(string categoryName, string effectName, out CategoryPool pool, out SFXEffect effect, out AudioClip clip)
        {
            pool = null;
            effect = null;
            clip = null;

            if (!_pools.TryGetValue(categoryName, out pool))
            {
                Debug.LogWarning($"[SFXPlayer] Category '{categoryName}' tidak ditemukan.");
                return false;
            }

            effect = sfxData.GetSFXEffect(categoryName, effectName);
            if (effect == null || effect.clips == null || effect.clips.Length == 0)
            {
                Debug.LogWarning($"[SFXPlayer] SFX '{categoryName}/{effectName}' tidak ditemukan.");
                return false;
            }

            if (CountPlaying(pool, effectName) >= effect.maxSimultaneous)
                return false;

            clip = effect.clips[Random.Range(0, effect.clips.Length)];
            return true;
        }
    }
}