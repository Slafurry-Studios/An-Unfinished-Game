using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using Slafurry.System.Audio;

public class AudioTestWindow : EditorWindow
    {
        private MusicData musicData;
        private SFXData sfxData;

        private string musicTrackName = "";
        private string sfxCategory = "";
        private string sfxEffect = "";

        private Vector2 scrollPos;

        private const string MasterKey = "MasterVolume";
        private const string MusicKey = "MusicVolume";
        private const string SFXKey = "SFXVolume";

        [MenuItem("Slafurry/Audio Test")]
        public static void ShowWindow()
        {
            GetWindow<AudioTestWindow>("Audio Test");
        }

        private void OnEnable()
        {
            musicData = AssetDatabase.LoadAssetAtPath<MusicData>("Assets/_Game/01_Objects/Data/Audio/Music/Music Data.asset");
            sfxData = AssetDatabase.LoadAssetAtPath<SFXData>("Assets/_Game/01_Objects/Data/Audio/SFX/Main SFX.asset");
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawMusicSection();
            EditorGUILayout.Space(20);
            DrawSFXSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawMusicSection()
        {
            EditorGUILayout.LabelField("Music", EditorStyles.boldLabel);

            musicTrackName = EditorGUILayout.TextField("Track Name", musicTrackName);

            float savedMusicVol = PlayerPrefs.GetFloat(MusicKey, 1f);
            EditorGUILayout.LabelField("PlayerPrefs Volume", savedMusicVol.ToString("F2"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
            {
                ApplyMixerVolume(savedMusicVol);
                var player = FindMusicPlayer();
                if (player != null)
                {
                    player.PlayMusic(musicTrackName, 0f);
                    Debug.Log($"[AudioTest] Play: {musicTrackName} (track={GetTrackVolume(musicTrackName)}, mixer={savedMusicVol})");
                }
                else
                {
                    Debug.LogWarning("[AudioTest] MusicPlayer not found.");
                }
            }
            if (GUILayout.Button("Stop"))
            {
                var player = FindMusicPlayer();
                if (player != null)
                {
                    player.StopMusic();
                    Debug.Log("[AudioTest] Stopped.");
                }
            }
            EditorGUILayout.EndHorizontal();

            if (musicData?.tracks != null)
            {
                EditorGUILayout.LabelField("Available:", string.Join(", ", System.Array.ConvertAll(musicData.tracks, t => t.trackName)), EditorStyles.miniLabel);
            }
        }

        private void DrawSFXSection()
        {
            EditorGUILayout.LabelField("SFX", EditorStyles.boldLabel);

            sfxCategory = EditorGUILayout.TextField("Category", sfxCategory);
            sfxEffect = EditorGUILayout.TextField("Effect", sfxEffect);

            float savedSFXVol = PlayerPrefs.GetFloat(SFXKey, 1f);
            EditorGUILayout.LabelField("PlayerPrefs Volume", savedSFXVol.ToString("F2"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
            {
                ApplyMixerVolume(savedSFXVol);
                var player = FindSFXPlayer();
                if (player != null)
                {
                    player.PlaySFX2D(sfxCategory, sfxEffect);
                    Debug.Log($"[AudioTest] Play SFX: {sfxCategory}/{sfxEffect} (mixer={savedSFXVol})");
                }
                else
                {
                    Debug.LogWarning("[AudioTest] SFXPlayer not found.");
                }
            }
            if (GUILayout.Button("Stop All"))
            {
                var player = FindSFXPlayer();
                if (player != null)
                {
                    player.StopAllSFX();
                    Debug.Log("[AudioTest] All SFX stopped.");
                }
            }
            EditorGUILayout.EndHorizontal();

            if (sfxData?.categories != null)
            {
                foreach (var cat in sfxData.categories)
                {
                    string effects = string.Join(", ", System.Array.ConvertAll(cat.effects, e => e.groupID));
                    EditorGUILayout.LabelField($"{cat.categoryName}:", effects, EditorStyles.miniLabel);
                }
            }
        }

        private void ApplyMixerVolume(float linearVolume)
        {
            var mixer = FindAudioMixer();
            if (mixer == null) return;

            float masterVol = PlayerPrefs.GetFloat(MasterKey, 1f);
            float dB = linearVolume > 0.0001f ? Mathf.Log10(linearVolume) * 20f : -80f;
            float masterDB = masterVol > 0.0001f ? Mathf.Log10(masterVol) * 20f : -80f;

            mixer.SetFloat(MasterKey, masterDB);
            mixer.SetFloat(MusicKey, dB);
            mixer.SetFloat(SFXKey, dB);
        }

        private AudioMixer FindAudioMixer()
        {
            var system = FindObjectOfType<AudioSystem>();
            if (system == null) return null;

            var field = typeof(AudioSystem).GetField("audioMixer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(system) as AudioMixer;
        }

        private float GetTrackVolume(string trackName)
        {
            if (musicData?.tracks == null) return 0f;
            foreach (var track in musicData.tracks)
            {
                if (track.trackName == trackName)
                    return track.volume;
            }
            return 0f;
        }

        private MusicPlayer FindMusicPlayer() => FindObjectOfType<MusicPlayer>();
        private SFXPlayer FindSFXPlayer() => FindObjectOfType<SFXPlayer>();
    }

