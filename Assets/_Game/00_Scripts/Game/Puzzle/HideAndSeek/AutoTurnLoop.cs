using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using Slafurry.System.Audio;

public class AutoTurnLoop : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] Transform visual;              // child sprite
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] bool artFacesRight = false;    // gambar aslinya hadap mana
    [SerializeField] bool startFacingRight = false; // hadap awal di scene

    [Header("Float")]
    [SerializeField] float floatAmplitude = 0.1f;
    [SerializeField] float floatSpeed = 2f;

    [Header("Timing")]
    [SerializeField] float holdTime = 2f;           // lama menghadap satu sisi
    [SerializeField] float windupTime = 0.5f;       // durasi turun lalu naik
    [SerializeField] float dipDepth = 0.3f;         // seberapa dalam turunnya

    [Header("Shake sebelum caught")]
    [SerializeField] float shakeTime = 0.6f;        // pemain harus bertahan segini lama
    [SerializeField] float shakeAmount = 0.08f;     // kekuatan getar

    [Header("Collider per sisi")]
    [SerializeField] List<Collider2D> rightColliders;
    [SerializeField] List<Collider2D> leftColliders;

    [Header("Events")]
    public UnityEvent onWindupStart;
    public UnityEvent onTurned;
    public UnityEvent onShakeStart;
    public UnityEvent onShakeCancel;
    public UnityEvent onCaught;

    bool facingRight;
    bool caught;
    Vector3 startPos;
    float dip;          // offset Y dari windup
    Vector3 shakeOffset;
    int insideCount;    // jumlah collider damage yang sedang berisi pemain
    Coroutine shakeRoutine;

    void Awake()
    {
        startPos = visual.localPosition;
    }

    void Start()
    {
        facingRight = startFacingRight;
        sprite.flipX = (facingRight != artFacesRight);
        RegisterRelays(rightColliders);
        RegisterRelays(leftColliders);
        ApplyColliders();
        StartCoroutine(Loop());
    }

    // ngambang + dip + shake digabung jadi satu posisi
    void Update()
    {
        float bob = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        visual.localPosition = startPos + Vector3.up * (bob + dip) + shakeOffset;
    }

    void RegisterRelays(List<Collider2D> list)
    {
        foreach (var c in list)
        {
            if (!c) continue;
            var r = c.GetComponent<CaughtRelay>();
            if (!r) r = c.gameObject.AddComponent<CaughtRelay>();
            r.Init(this);
        }
    }

    void ApplyColliders()
    {
        SetAll(rightColliders, facingRight);
        SetAll(leftColliders, !facingRight);
    }

    void SetAll(List<Collider2D> list, bool on)
    {
        foreach (var c in list) if (c) c.enabled = on;
    }

    // ---- dipanggil CaughtRelay ----
    public void OnPlayerInside()
    {
        if (caught || shakeRoutine != null) return;
        shakeRoutine = StartCoroutine(ShakeThenCatch());
    }

    // dipanggil relay saat pemain keluar, atau saat collider dimatikan
    public void OnPlayerLeft()
    {
        if (shakeRoutine == null) return;
        CancelShake();
    }

    void CancelShake()
    {
        StopCoroutine(shakeRoutine);
        shakeRoutine = null;
        shakeOffset = Vector3.zero;
        onShakeCancel.Invoke();
        Audio.StopSFX("UI", "Windup");
        // AUDIO: stop/cancel suara tegang di sini (one-liner sistem audio)
    }

    IEnumerator ShakeThenCatch()
    {
        onShakeStart.Invoke();
        Audio.PlaySFX2D("UI", "Windup");
        float t = 0f;
        while (t < shakeTime)
        {
            t += Time.deltaTime;
            shakeOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f), 0f) * shakeAmount;
            yield return null;
        }

        shakeOffset = Vector3.zero;
        shakeRoutine = null;
        caught = true;
        onCaught.Invoke();
        Audio.StopSFX("UI", "Windup");
        Audio.PlaySFX2D("UI", "Jumpscare");

    }

    // panggil dari respawn/restart checkpoint
    public void ResetCaught()
    {
        caught = false;
        if (shakeRoutine != null) CancelShake();
    }

    IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitForSeconds(holdTime);

            // windup: dua sisi aman selagi bersiap
            SetAll(rightColliders, false);
            SetAll(leftColliders, false);
            // collider dimatikan = pemain dianggap keluar, batalkan shake
            if (shakeRoutine != null) CancelShake();
            onWindupStart.Invoke();
            // AUDIO: suara balik badan/windup di sini (one-liner sistem audio)

            bool flipped = false;
            float t = 0;
            while (t < windupTime)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / windupTime);
                dip = -dipDepth * Mathf.Sin(p * Mathf.PI);   // turun lalu naik

                // balik badan di titik terendah
                if (!flipped && p >= 0.5f)
                {
                    facingRight = !facingRight;
                    sprite.flipX = (facingRight != artFacesRight);
                    flipped = true;
                }
                yield return null;
            }
            dip = 0f;

            ApplyColliders();
            onTurned.Invoke();
        }
    }
}

