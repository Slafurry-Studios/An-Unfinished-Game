using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmGame
{
    /// <summary>
    /// Note versi UI: pakai RectTransform + Image, HARUS jadi child dari Canvas
    /// (lewat noteContainer di NoteSpawner). Posisi dihitung dari selisih waktu
    /// terhadap lagu (bukan physics), supaya selalu sinkron walau scrollSpeed
    /// berubah live.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class Note : MonoBehaviour
    {
        public int lane;
        public float hitTime;

        [HideInInspector] public bool judged;

        private RectTransform _rect;
        private Image _image;
        private float _hitY;
        private float _laneX;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
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

            float y = _hitY + (hitTime - songTime) * speed;
            _rect.anchoredPosition = new Vector2(_laneX, y);

            if (!judged && songTime > hitTime + LaneHitZone.MissWindow)
            {
                judged = true;
                ScoreManager.Instance.RegisterMiss();
                NoteSpawner.Instance.RemoveActiveNote(this);
                Destroy(gameObject);
            }
        }

        public void PlayHitPunch()
        {
            StartCoroutine(HitPunchRoutine());
        }

        private IEnumerator HitPunchRoutine()
        {
            float duration = 0.4f;
            float punchScale = 1.35f;
            Vector3 originalScale = _rect.localScale;
            Vector3 punchTarget = originalScale * punchScale;
            Color originalColor = _image.color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Scale: punch up then ease back
                float scaleCurve = 1f - (1f - t) * (1f - t);
                _rect.localScale = Vector3.Lerp(punchTarget, originalScale, scaleCurve);

                // Fade out: linear
                _image.color = new Color(originalColor.r, originalColor.g, originalColor.b,
                                         Mathf.Lerp(1f, 0f, t));

                yield return null;
            }

            _rect.localScale = originalScale;
            _image.color = originalColor;
            Destroy(gameObject);
        }
    }
}