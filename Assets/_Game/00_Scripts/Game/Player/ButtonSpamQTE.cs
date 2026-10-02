using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using Slafurry.System.Audio;

public class ButtonSpamQTE : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] KeyCode key = KeyCode.E;

    [Header("Tuning")]
    [SerializeField] int targetPresses = 15;
    [SerializeField] float timeLimit = 5f;
    [SerializeField] float decayPerSecond = 1.5f;
    [SerializeField] float gainPerPress = 1f;
    [SerializeField] float successHoldTime = 0.6f;   // sprite fase 3 tampil segini lama

    [Header("UI")]
    [SerializeField] Slider slider;                  // progres 0..1 (opsional)
    [SerializeField] GameObject sliderRoot;          // opsional, ditampilkan saat QTE

    [Header("Sprite per fase (isi 4: 0,1,2, lalu 3 = berhasil)")]
    [SerializeField] Sprite[] phaseSprites = new Sprite[4];
    [SerializeField] SpriteRenderer spriteRenderer;  // pakai salah satu: SpriteRenderer...
    [SerializeField] Image uiImage;                  // ...atau UI Image

    [Header("Shake")]
    [SerializeField] Transform shakeTarget;          // objek yang digetarkan
    [SerializeField] float baseShake = 0.02f;        // getar dasar
    [SerializeField] float shakeByProgress = 0.08f;  // tambahan makin dekat berhasil
    [SerializeField] float punchOnPress = 0.1f;      // getar kuat sesaat tiap tekan
    [SerializeField] float punchDecay = 12f;

    [Header("Events")]
    public UnityEvent onStart;
    public UnityEvent<int> onPhaseChanged;           // 0..3 (kalau tidak muncul di Inspector, hapus saja)
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    float progress, timer, punch;
    bool active;
    int phase = -1;
    Vector3 shakeBasePos;

    public void Begin()
    {
        StopAllCoroutines();
        progress = 0f;
        timer = timeLimit;
        punch = 0f;
        phase = -1;
        active = true;
        if (shakeTarget) shakeBasePos = shakeTarget.localPosition;
        if (sliderRoot) sliderRoot.SetActive(true);
        SetPhase(0);
        Audio.PlaySFX2D("Entity", "Jumpscare", loop: true);
        UpdateSlider();
        onStart.Invoke();
    }

    public void Cancel()
    {
        active = false;
        EndVisuals();
    }

    void Update()
    {
        if (!active) return;

        if (Input.GetKeyDown(key))
        {
            progress += gainPerPress;
            punch = punchOnPress;
            Audio.PlaySFX2D("UI", "ButtonLast");
        }

        progress = Mathf.Max(0f, progress - decayPerSecond * Time.deltaTime);
        timer -= Time.deltaTime;

        float p = Mathf.Clamp01(progress / targetPresses);
        UpdateSlider();
        UpdatePhase(p);
        UpdateShake(p);

        if (progress >= targetPresses)
        {
            active = false;
            StartCoroutine(SuccessRoutine());
        }
        else if (timer <= 0f)
        {
            active = false;
            EndVisuals();
            onFail.Invoke();
            // AUDIO: suara gagal di sini
            Audio.StopSFX("Entity", "Jumpscare", fadeOut: 1f);
        }
    }

    void UpdateSlider()
    {
        if (slider) slider.value = Mathf.Clamp01(progress / targetPresses);
    }

    void UpdatePhase(float p)
    {
        int newPhase = p < 0.33f ? 0 : (p < 0.66f ? 1 : 2);
        if (newPhase != phase) SetPhase(newPhase);
    }

    void SetPhase(int i)
    {
        phase = i;
        if (phaseSprites != null && i < phaseSprites.Length && phaseSprites[i])
        {
            if (spriteRenderer) spriteRenderer.sprite = phaseSprites[i];
            if (uiImage) uiImage.sprite = phaseSprites[i];
        }
        onPhaseChanged.Invoke(i);
        // AUDIO: suara pergantian fase di sini
    }

    void UpdateShake(float p)
    {
        if (!shakeTarget) return;
        punch = Mathf.Lerp(punch, 0f, punchDecay * Time.deltaTime);
        float amount = baseShake + shakeByProgress * p + punch;
        shakeTarget.localPosition = shakeBasePos +
            (Vector3)(Random.insideUnitCircle * amount);
    }

    IEnumerator SuccessRoutine()
    {
        SetPhase(3);                 // sprite berhasil
        if (slider) slider.value = 1f;
        if (shakeTarget) shakeTarget.localPosition = shakeBasePos;
        // AUDIO: suara lolos di sini
        yield return new WaitForSeconds(successHoldTime);
        EndVisuals();
        Audio.StopSFX("Entity", "Jumpscare", fadeOut: 1f);
        onSuccess.Invoke();
    }

    void EndVisuals()
    {
        if (shakeTarget) shakeTarget.localPosition = shakeBasePos;
        if (sliderRoot) sliderRoot.SetActive(false);
    }
}