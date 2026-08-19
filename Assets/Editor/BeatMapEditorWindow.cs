using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RhythmGame.EditorTools
{
    /// <summary>
    /// Buka lewat menu: RhythmGame > Beat Map Editor.
    /// Alat bantu buat "nge-tap" beat sambil dengar lagu langsung di Editor
    /// (tidak perlu masuk Play Mode), lalu otomatis disimpan ke SongChart.
    ///
    /// Cara pakai singkat:
    /// 1. Assign SongChart & AudioClip.
    /// 2. Tekan Space untuk play/pause preview lagu.
    /// 3. Tekan angka 1-4 untuk pilih lane.
    /// 4. Tekan Enter (atau klik tombol "Tap!") persis saat dengar beat
    ///    yang mau dijadikan note -> otomatis tercatat waktu + lane-nya.
    /// 5. Note otomatis masuk ke list SongChart.notes, tinggal Ctrl+S / save asset.
    ///
    /// CATATAN: script ini pakai reflection ke UnityEditor.AudioUtil (API internal
    /// Unity yang dipakai buat preview audio di Editor, bukan API publik resmi).
    /// Nama method internal ini kadang beda antar versi Unity. Kalau ada error
    /// "method not found" di Console, cek daftar nama alternatif di komentar
    /// bagian bawah file ini dan sesuaikan.
    /// </summary>
    public class BeatMapEditorWindow : EditorWindow
    {
        private SongChart _chart;
        private AudioClip _clip;
        private int _selectedLane;
        private bool _isPlaying;

        private bool _snapToGrid = true;
        private readonly string[] _subdivisionLabels = { "1/1 (ketuk)", "1/2", "1/4", "1/8" };
        private readonly int[] _subdivisionValues = { 1, 2, 4, 8 };
        private int _subdivisionIndex = 2; // default 1/4

        private static Type _audioUtilType;
        private static Type AudioUtilType =>
            _audioUtilType ??= typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

        [MenuItem("Tools/RhythmGame/Beat Map Editor")]
        public static void ShowWindow()
        {
            GetWindow<BeatMapEditorWindow>("Beat Map Editor");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopClip();
        }

        private void OnEditorUpdate()
        {
            if (_isPlaying) Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Beat Map Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _chart = (SongChart)EditorGUILayout.ObjectField("Song Chart", _chart, typeof(SongChart), false);
            _clip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip (preview)", _clip, typeof(AudioClip), false);

            if (_chart == null || _clip == null)
            {
                EditorGUILayout.HelpBox("Assign SongChart dan AudioClip dulu ya.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            float pos = GetClipPosition();
            EditorGUILayout.LabelField($"Posisi: {pos:0.000}s / {_clip.length:0.000}s");
            Rect barRect = EditorGUILayout.GetControlRect(false, 6);
            EditorGUI.ProgressBar(barRect, _clip.length > 0 ? pos / _clip.length : 0f, "");

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_isPlaying ? "❚❚ Pause (Space)" : "► Play (Space)", GUILayout.Height(30)))
                TogglePlay();
            if (GUILayout.Button("■ Stop", GUILayout.Height(30), GUILayout.Width(80)))
                StopClip();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lane aktif (tombol 1-4):");
            _selectedLane = GUILayout.SelectionGrid(_selectedLane, new[] { "1", "2", "3", "4" }, 4, GUILayout.Height(28));

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            _snapToGrid = EditorGUILayout.ToggleLeft("Snap ke BPM Grid", _snapToGrid, GUILayout.Width(140));
            using (new EditorGUI.DisabledScope(!_snapToGrid))
            {
                _subdivisionIndex = EditorGUILayout.Popup(_subdivisionIndex, _subdivisionLabels);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"BPM chart: {_chart.bpm}", EditorStyles.miniLabel);

            EditorGUILayout.Space();
            if (GUILayout.Button("TAP! Tambah Note Di Posisi Ini  (Enter)", GUILayout.Height(45)))
                AddNoteAtCurrentPosition();

            HandleKeyboardShortcuts();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Jumlah note tersimpan: {_chart.notes.Count}");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Urutkan Waktu"))
            {
                _chart.notes.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));
                EditorUtility.SetDirty(_chart);
            }
            if (GUILayout.Button("Hapus Terakhir") && _chart.notes.Count > 0)
            {
                _chart.notes.RemoveAt(_chart.notes.Count - 1);
                EditorUtility.SetDirty(_chart);
            }
            if (GUILayout.Button("Hapus Semua"))
            {
                if (EditorUtility.DisplayDialog("Konfirmasi", "Hapus semua note di chart ini?", "Ya, hapus", "Batal"))
                {
                    _chart.notes.Clear();
                    EditorUtility.SetDirty(_chart);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Space = Play/Pause | 1-4 = pilih lane | Enter = tap note.\n" +
                "Jangan lupa Ctrl+S (Save Project) biar chart-nya kesimpan permanen.",
                MessageType.None);
        }

        private void HandleKeyboardShortcuts()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.Space)
            {
                TogglePlay();
                e.Use();
            }
            else if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha4)
            {
                _selectedLane = e.keyCode - KeyCode.Alpha1;
                e.Use();
                Repaint();
            }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                AddNoteAtCurrentPosition();
                e.Use();
            }
        }

        private void AddNoteAtCurrentPosition()
        {
            float pos = GetClipPosition();

            if (_snapToGrid && _chart.bpm > 0f)
            {
                float subdivision = _subdivisionValues[_subdivisionIndex];
                float beatLength = 60f / _chart.bpm / subdivision;
                pos = Mathf.Round(pos / beatLength) * beatLength;
            }

            _chart.notes.Add(new NoteData { lane = _selectedLane, hitTime = pos });
            EditorUtility.SetDirty(_chart);
            ShowNotification(new GUIContent($"Note lane {_selectedLane + 1} @ {pos:0.000}s"));
        }

        // ---------------------------------------------------------------
        // Reflection ke UnityEditor.AudioUtil (internal API preview audio)
        // ---------------------------------------------------------------

        private void TogglePlay()
        {
            if (_isPlaying) PauseClip();
            else PlayClip();
        }

        private void PlayClip()
        {
            if (_clip == null) return;
            InvokeAudioUtil(new[] { "PlayPreviewClip", "PlayClip" },
                new object[] { _clip, 0, false });
            _isPlaying = true;
        }

        private void PauseClip()
        {
            InvokeAudioUtil(new[] { "PausePreviewClip", "PauseClip" }, Array.Empty<object>());
            _isPlaying = false;
        }

        private void StopClip()
        {
            InvokeAudioUtil(new[] { "StopAllPreviewClips", "StopAllClips" }, Array.Empty<object>());
            _isPlaying = false;
        }

        private float GetClipPosition()
        {
            if (_clip == null) return 0f;
            object result = InvokeAudioUtil(new[] { "GetPreviewClipPosition", "GetClipPosition" }, Array.Empty<object>());
            return result != null ? Convert.ToSingle(result) : 0f;
        }

        /// <summary>
        /// Coba panggil salah satu nama method dari daftar kandidat (beda versi
        /// Unity kadang pakai nama beda). Kalau semua gagal, tampilkan warning
        /// sekali di Console supaya kamu tahu perlu sesuaikan nama method.
        /// </summary>
        private object InvokeAudioUtil(string[] candidateNames, object[] args)
        {
            if (AudioUtilType == null) return null;

            foreach (var name in candidateNames)
            {
                MethodInfo method = AudioUtilType.GetMethod(name,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null) continue;

                try
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length == args.Length
                        ? method.Invoke(null, args)
                        : method.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BeatMapEditorWindow] Gagal invoke {name}: {ex.Message}");
                }
            }
            return null;
        }
    }
}

/*
 * Kalau muncul error/warning "method not found" di Console karena versi Unity
 * kamu beda, ini daftar nama alternatif yang pernah dipakai Unity di berbagai
 * versi untuk UnityEditor.AudioUtil (coba ganti di array candidateNames):
 *
 *   Play    : "PlayPreviewClip", "PlayClip"
 *   Pause   : "PausePreviewClip", "PauseClip"
 *   Stop    : "StopAllPreviewClips", "StopAllClips"
 *   Posisi  : "GetPreviewClipPosition", "GetClipPosition", "GetClipSamplePosition"
 *             (kalau yang kepakai "GetClipSamplePosition", hasilnya dalam SAMPLE,
 *              bukan detik -> bagi dengan clip.frequency untuk dapat detik)
 *   IsPlaying: "IsPreviewClipPlaying", "IsClipPlaying"
 */