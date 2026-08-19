using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    [Serializable]
    public class NoteData
    {
        [Tooltip("Lane 0-3 (0 = paling kiri, 3 = paling kanan)")]
        [Range(0, 3)]
        public int lane;

        [Tooltip("Waktu note ini harus di-hit, dalam detik sejak lagu mulai (bukan sejak game start)")]
        public float hitTime;
    }

    /// <summary>
    /// Klik kanan di Project window -> Create -> RhythmGame -> Song Chart
    /// untuk membuat asset chart baru. Isi field "notes" secara manual di Inspector,
    /// atau generate dari script/JSON kalau chart-nya panjang.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSongChart", menuName = "RhythmGame/Song Chart")]
    public class SongChart : ScriptableObject
    {
        public string songName;
        public float bpm = 120f;
        public List<NoteData> notes = new List<NoteData>();

        [ContextMenu("Urutkan Note Berdasarkan Waktu")]
        private void SortNotes()
        {
            notes.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));
        }
    }
}