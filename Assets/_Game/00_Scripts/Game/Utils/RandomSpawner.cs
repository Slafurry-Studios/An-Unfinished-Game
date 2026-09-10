using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Slafurry.System.Audio;

public class RandomSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject prefab;

    [Header("Spawn Settings")]
    [SerializeField] private int maxCount = 10;
    [SerializeField] private float minDistance = 50f;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private float spawnRadius = 100f;

    [Header("Audio")]
    [SerializeField] private string sfxCategory = "SFX";
    [SerializeField] private string sfxEffect = "Spawn";

    [Header("Events")]
    [SerializeField] private UnityEvent onEnd;

    private readonly List<RectTransform> _spawned = new List<RectTransform>();
    private Coroutine _spawnRoutine;
    private RectTransform _canvasRect;

    private void Awake()
    {
        _canvasRect = canvas.GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (_spawnRoutine != null)
            StopCoroutine(_spawnRoutine);
    }

    private IEnumerator SpawnLoop()
    {
        while (_spawned.Count < maxCount)
        {
            Vector2 pos = GetRandomPosition();

            if (pos != Vector2.zero)
                Spawn(pos);

            yield return new WaitForSeconds(spawnInterval);
        }

        onEnd?.Invoke();
    }

    private void Spawn(Vector2 screenPos)
    {
        GameObject obj = Instantiate(prefab, canvas.transform);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = screenPos;
        rt.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        _spawned.Add(rt);

        if (AudioSystem.Instance != null)
            Audio.PlaySFX2D(sfxCategory, sfxEffect);
    }

    private Vector2 GetRandomPosition()
    {
        Rect canvasRect = _canvasRect.rect;
        float halfW = canvasRect.width * 0.5f;
        float halfH = canvasRect.height * 0.5f;

        for (int i = 0; i < 20; i++)
        {
            float x = Random.Range(-halfW + spawnRadius, halfW - spawnRadius);
            float y = Random.Range(-halfH + spawnRadius, halfH - spawnRadius);
            Vector2 candidate = new Vector2(x, y);

            if (IsClear(candidate))
                return candidate;
        }

        return Vector2.zero;
    }

    private bool IsClear(Vector2 candidate)
    {
        foreach (var rt in _spawned)
        {
            if (rt == null) continue;
            if (Vector2.Distance(rt.anchoredPosition, candidate) < minDistance)
                return false;
        }
        return true;
    }

    public void Clear()
    {
        foreach (var rt in _spawned)
        {
            if (rt != null)
                Destroy(rt.gameObject);
        }
        _spawned.Clear();
    }
}
