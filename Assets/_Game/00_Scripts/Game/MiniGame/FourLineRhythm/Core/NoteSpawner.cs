using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmGame
{
    public class NoteSpawner : MonoBehaviour
    {
        public static NoteSpawner Instance { get; internal set; }

        [Header("Referensi")]
        public SongChart chart;
        public GameObject notePrefab;
        public Conductor conductor;
        public RectTransform noteContainer;

        [Header("Titik Hit (tempat kamu harus menekan tombol)")]
        [Tooltip("Drag 4 GameObject penanda hit point, urut lane 0-3.")]
        public RectTransform[] laneHitPoints = new RectTransform[4];

        [Header("Titik Spawn (tempat note pertama kali muncul)")]
        [Tooltip("Drag 4 GameObject penanda titik spawn, urut lane 0-3.")]
        public RectTransform[] laneSpawnPoints = new RectTransform[4];

        private int _nextNoteIndex;
        private readonly List<Note>[] _activeNotesPerLane = new List<Note>[4];

        private Vector2[] _cachedHitLocalPos;
        private Vector2[] _cachedSpawnLocalPos;
        private bool _positionsCached;
        private bool _waitingToCache;

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < 4; i++) _activeNotesPerLane[i] = new List<Note>();
        }

        /// <summary>
        /// Tandai bahwa posisi perlu di-cache di frame berikutnya.
        /// Dipanggil dari PlayGame() supaya frame pertama sudah pakai posisi benar.
        /// </summary>
        public void InvalidateCache()
        {
            _positionsCached = false;
            _waitingToCache = true;
        }

        private void LateUpdate()
        {
            if (_waitingToCache && !_positionsCached)
            {
                _waitingToCache = false;
                CachePositions();
            }
        }

        private void CachePositions()
        {
            if (noteContainer == null) return;

            // Force rebuild layout HIERARCHY dulu, bukan cuma canvas global
            var layoutRoot = noteContainer.GetComponentInParent<HorizontalLayoutGroup>();
            if (layoutRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot.GetComponent<RectTransform>());

            // juga rebuild noteContainer sendiri kalau dia punya layout
            var selfLayout = noteContainer.GetComponent<LayoutGroup>();
            if (selfLayout != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(noteContainer);

            _cachedHitLocalPos = new Vector2[laneHitPoints.Length];
            _cachedSpawnLocalPos = new Vector2[laneSpawnPoints.Length];

            for (int i = 0; i < laneHitPoints.Length; i++)
            {
                if (laneHitPoints[i] != null)
                {
                    laneHitPoints[i].ForceUpdateRectTransforms();
                    _cachedHitLocalPos[i] = noteContainer.InverseTransformPoint(laneHitPoints[i].position);
                }
            }

            for (int i = 0; i < laneSpawnPoints.Length; i++)
            {
                if (laneSpawnPoints[i] != null)
                {
                    laneSpawnPoints[i].ForceUpdateRectTransforms();
                    _cachedSpawnLocalPos[i] = noteContainer.InverseTransformPoint(laneSpawnPoints[i].position);
                }
            }

            _positionsCached = true;

            Debug.Log($"[NoteSpawner] CachePositions done. Hit[0]={_cachedHitLocalPos[0]}, Spawn[0]={_cachedSpawnLocalPos[0]}");
        }

        private void Update()
        {
            if (chart == null || conductor == null || !conductor.HasStarted) return;
            if (!_positionsCached) return;

            float songTime = conductor.GetSongTime();

            while (_nextNoteIndex < chart.notes.Count)
            {
                NoteData data = chart.notes[_nextNoteIndex];
                int lane = Mathf.Clamp(data.lane, 0, laneHitPoints.Length - 1);
                float leadTime = GetLeadTime(lane);

                if (data.hitTime - leadTime > songTime) break;

                SpawnNote(data, lane);
                _nextNoteIndex++;
            }
        }

        private float GetLeadTime(int lane)
        {
            if (_cachedHitLocalPos == null || _cachedSpawnLocalPos == null) return 1f;
            if (lane >= _cachedHitLocalPos.Length || lane >= _cachedSpawnLocalPos.Length) return 1f;

            float hitY = _cachedHitLocalPos[lane].y;
            float spawnY = _cachedSpawnLocalPos[lane].y;
            float distance = Mathf.Abs(spawnY - hitY);

            return distance / Mathf.Max(conductor.scrollSpeed, 0.01f);
        }

        private void SpawnNote(NoteData data, int lane)
        {
            if (_cachedHitLocalPos == null || lane >= _cachedHitLocalPos.Length)
            {
                Debug.LogWarning($"[NoteSpawner] laneHitPoints[{lane}] belum di-assign! Note dilewati.");
                return;
            }

            Vector2 hitLocalPos = _cachedHitLocalPos[lane];

            GameObject obj = Instantiate(notePrefab, noteContainer);
            Note note = obj.GetComponent<Note>();
            note.Init(lane, hitLocalPos.x, hitLocalPos.y, data.hitTime);
            _activeNotesPerLane[lane].Add(note);
        }

        public List<Note> GetActiveNotesInLane(int lane) => _activeNotesPerLane[lane];

        public void RemoveActiveNote(Note note)
        {
            _activeNotesPerLane[note.lane].Remove(note);
        }

        /// <summary>True kalau semua note di chart sudah selesai di-spawn (tidak berarti sudah dinilai).</summary>
        public bool AllNotesSpawned => chart != null && _nextNoteIndex >= chart.notes.Count;

        /// <summary>True kalau masih ada note yang tampil di layar (belum di-hit / di-miss).</summary>
        public bool HasActiveNotes()
        {
            for (int i = 0; i < _activeNotesPerLane.Length; i++)
                if (_activeNotesPerLane[i].Count > 0) return true;
            return false;
        }

        /// <summary>True kalau chart sudah benar-benar selesai: semua note sudah di-spawn DAN sudah dinilai (hit/miss), tidak ada sisa di layar. Dipakai GameManager untuk mendeteksi kondisi MENANG.</summary>
        public bool IsChartFinished => AllNotesSpawned && !HasActiveNotes();

        /// <summary>Reset spawner supaya bisa main ulang dari awal (dipanggil GameManager.PlayGame()).</summary>
        public void ResetSpawner()
        {
            _nextNoteIndex = 0;
            for (int i = 0; i < _activeNotesPerLane.Length; i++)
            {
                foreach (var n in _activeNotesPerLane[i])
                    if (n != null) Destroy(n.gameObject);
                _activeNotesPerLane[i].Clear();
            }
        }
    }
}