using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Note versi UI: pakai RectTransform + Image, HARUS jadi child dari Canvas
    /// (lewat noteContainer di NoteSpawner). Posisi dihitung dari selisih waktu
    /// terhadap lagu (bukan physics), supaya selalu sinkron walau scrollSpeed
    /// berubah live.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class Note : MonoBehaviour
    {
        public int lane;
        public float hitTime;

        [HideInInspector] public bool judged;

        private RectTransform _rect;
        private float _hitY;
        private float _laneX;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        public void Init(int laneIndex, float laneX, float hitY, float targetHitTime)
        {
            lane = laneIndex;
            _laneX = laneX;
            _hitY = hitY;
            hitTime = targetHitTime;
        }

        private void Update()
        {
            if (Conductor.Instance == null) return;

            float songTime = Conductor.Instance.GetSongTime();
            float speed = Conductor.Instance.scrollSpeed;

            // anchoredPosition dihitung ulang tiap frame dari selisih waktu,
            // jadi note selalu sinkron dengan lagu walau scrollSpeed berubah live.
            float y = _hitY + (hitTime - songTime) * speed;
            _rect.anchoredPosition = new Vector2(_laneX, y);

            // Kalau note sudah lewat jauh dari hit line dan belum dinilai -> MISS
            if (!judged && songTime > hitTime + LaneHitZone.MissWindow)
            {
                judged = true;
                ScoreManager.Instance.RegisterMiss();
                NoteSpawner.Instance.RemoveActiveNote(this);
                Destroy(gameObject);
            }
        }
    }
}