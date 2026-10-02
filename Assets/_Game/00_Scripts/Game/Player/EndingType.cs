using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections;
using Slafurry.System.Audio;

[Serializable]
class TypePhase
{
    [TextArea] public string text;
    public float charDelay = 0.06f;     // jeda antar huruf
    public float holdAfter = 1.2f;      // tahan setelah selesai ngetik
    public bool clearBefore = true;     // hapus teks sebelumnya sebelum mulai
    public UnityEvent onPhaseStart;     // atur efek per fase di sini (desain kamu)
    public UnityEvent onPhaseTyped;     // saat teks fase ini selesai diketik
}

public class EndingType : MonoBehaviour
{
    [SerializeField] TMP_Text label;
    [SerializeField] bool playOnStart = false;
    [SerializeField] KeyCode skipKey = KeyCode.Space;   // tekan untuk melompat ke akhir fase

    [SerializeField]
    TypePhase[] phases = new TypePhase[]
    {
        new TypePhase { text = "I will keep making games" },
        new TypePhase { text = "Because I love it" },
        new TypePhase { text = "Masterpiece or not", charDelay = 0.09f, holdAfter = 2.5f },
    };

    public UnityEvent onCharTyped;      // sambungkan ke blip audio (one-liner sistemmu)
    public UnityEvent onAllDone;        // misal lanjut ke credits / tombol PUBLISH

    Coroutine routine;

    void Start() { if (playOnStart) Play(); }

    public void Play()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        label.text = "";

        for (int i = 0; i < phases.Length; i++)
        {
            var p = phases[i];

            if (p.clearBefore)
                label.text = "";

            // AUDIO: suara awal fase
            p.onPhaseStart.Invoke();

            string prefix = p.clearBefore ? "" : label.text + "\n";
            label.text = prefix + p.text;
            label.ForceMeshUpdate();

            int start = p.clearBefore ? 0 : prefix.Length;
            int total = label.textInfo.characterCount;
            label.maxVisibleCharacters = start;

            // ▶ MULAI suara typing
            Audio.PlaySFX2D("UI", "typing", loop: true);

            for (int c = start; c < total; c++)
            {
                if (Input.GetKeyDown(skipKey))
                    break;

                label.maxVisibleCharacters = c + 1;

                onCharTyped.Invoke();

                yield return new WaitForSecondsRealtime(p.charDelay);
            }

            label.maxVisibleCharacters = total;

            // ■ STOP suara typing
            Audio.StopSFX("UI", "typing");

            p.onPhaseTyped.Invoke();

            yield return new WaitForSecondsRealtime(p.holdAfter);
        }

        routine = null;
        onAllDone.Invoke();
    }
}