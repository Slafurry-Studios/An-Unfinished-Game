using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Tempel 1 script ini per lane (jadi 4 GameObject total, di posisi hit line
    /// masing-masing lane). Mendeteksi input keyboard dan menilai note terdekat
    /// di lane tersebut (Perfect / Good / Ok / Miss).
    /// </summary>
    public class LaneHitZone : MonoBehaviour
    {
        public const float PerfectWindow = 0.05f; // detik
        public const float GoodWindow = 0.10f;
        public const float MissWindow = 0.15f;

        [Tooltip("Index lane, 0-3, harus cocok dengan NoteData.lane")]
        public int laneIndex;

        [Tooltip("Tombol keyboard untuk lane ini, contoh: D, F, J, K")]
        public KeyCode key = KeyCode.D;

        [Header("Feedback (opsional)")]
        public Animator pressAnimator; // trigger animasi tombol saat ditekan, boleh kosong

        private void Update()
        {
            if (Input.GetKeyDown(key))
            {
                if (pressAnimator != null) pressAnimator.SetTrigger("Press");
                TryHit();
            }
        }

        private void TryHit()
        {
            var notes = NoteSpawner.Instance.GetActiveNotesInLane(laneIndex);
            if (notes.Count == 0) return;

            float songTime = Conductor.Instance.GetSongTime();

            // Cari note dengan selisih waktu paling kecil di lane ini
            Note closest = null;
            float closestDiff = float.MaxValue;
            foreach (var n in notes)
            {
                if (n.judged) continue;
                float diff = Mathf.Abs(n.hitTime - songTime);
                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closest = n;
                }
            }

            if (closest == null || closestDiff > MissWindow) return; // tidak ada note yang cukup dekat

            closest.judged = true;

            if (closestDiff <= PerfectWindow) ScoreManager.Instance.RegisterHit(Judgement.Perfect);
            else if (closestDiff <= GoodWindow) ScoreManager.Instance.RegisterHit(Judgement.Good);
            else ScoreManager.Instance.RegisterHit(Judgement.Ok);

            NoteSpawner.Instance.RemoveActiveNote(closest);
            Destroy(closest.gameObject);
        }
    }
}