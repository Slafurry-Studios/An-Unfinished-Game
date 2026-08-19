using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    public class NoteSpawner : MonoBehaviour
    {
        public static NoteSpawner Instance { get; private set; }

        [Header("Referensi")]
        public SongChart chart;
        public GameObject notePrefab;         // Prefab UI: RectTransform + Image + Note.cs
        public Conductor conductor;
        public RectTransform noteContainer;   // RectTransform kosong, PARENT semua note (JANGAN taruh di dalam Horizontal Layout Group!)

        [Header("Titik Hit (tempat kamu harus menekan tombol)")]
        [Tooltip("Drag 4 GameObject penanda hit point, urut lane 0-3. Note akan berhenti tepat di posisi ini saat waktunya pas (hitTime).")]
        public RectTransform[] laneHitPoints = new RectTransform[4];

        [Header("Titik Spawn (tempat note pertama kali muncul)")]
        [Tooltip("Drag 4 GameObject penanda titik spawn, urut lane 0-3. Boleh 1 objek yang sama untuk semua lane (misal garis 'SpawnLine' di atas layar), atau beda-beda per lane kalau mau.")]
        public RectTransform[] laneSpawnPoints = new RectTransform[4];

        private int _nextNoteIndex;
        private readonly List<Note>[] _activeNotesPerLane = new List<Note>[4];

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < 4; i++) _activeNotesPerLane[i] = new List<Note>();
        }

        private void Update()
        {
            if (chart == null) return;

            float songTime = conductor.GetSongTime();

            while (_nextNoteIndex < chart.notes.Count)
            {
                NoteData data = chart.notes[_nextNoteIndex];
                int lane = Mathf.Clamp(data.lane, 0, laneHitPoints.Length - 1);
                float leadTime = GetLeadTime(lane);

                // Belum waktunya spawn note ini -> berhenti cek, tunggu frame berikutnya
                if (data.hitTime - leadTime > songTime) break;

                SpawnNote(data, lane);
                _nextNoteIndex++;
            }
        }

        /// <summary>
        /// Berapa detik note butuh untuk jalan dari titik spawn ke titik hit,
        /// dihitung langsung dari JARAK ANTAR OBJEK (bukan angka manual),
        /// dibagi scrollSpeed saat ini. Dihitung ulang tiap kali dipanggil,
        /// jadi otomatis akurat walau speed berubah live atau posisi objek digeser.
        /// </summary>
        private float GetLeadTime(int lane)
        {
            RectTransform hit = laneHitPoints[lane];
            RectTransform spawn = laneSpawnPoints[lane];
            if (hit == null || spawn == null || noteContainer == null) return 1f;

            float hitY = noteContainer.InverseTransformPoint(hit.position).y;
            float spawnY = noteContainer.InverseTransformPoint(spawn.position).y;
            float distance = Mathf.Abs(spawnY - hitY);

            return distance / Mathf.Max(conductor.scrollSpeed, 0.01f);
        }

        private void SpawnNote(NoteData data, int lane)
        {
            RectTransform hit = laneHitPoints[lane];
            if (hit == null)
            {
                Debug.LogWarning($"[NoteSpawner] laneHitPoints[{lane}] belum di-assign! Note dilewati.");
                return;
            }

            // Posisi X & Y target (hit point) diambil langsung dari objek yang
            // kamu taruh di scene, dikonversi ke local space milik noteContainer.
            Vector2 hitLocalPos = noteContainer.InverseTransformPoint(hit.position);

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
    }
}