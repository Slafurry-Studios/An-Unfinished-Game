using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    public int CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }

    public UnityEvent OnTakeDamage;
    public UnityEvent OnDied;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Range(0f, 1f)] private float blinkAlpha = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private bool _isInvincible;
    private Coroutine _invincibilityCoroutine;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (debug && Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || _isInvincible)
            return;

        damage = Mathf.Max(0, damage);

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnTakeDamage?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        StopInvincibility();

        Debug.Log("Player has died.");

        OnDied?.Invoke();
    }

    /// <summary>
    /// Starts temporary invincibility and the blinking visual effect.
    /// Can be called directly from UnityEvent.
    /// </summary>
    public void StartInvincibility()
    {
        if (IsDead)
            return;

        if (_invincibilityCoroutine != null)
            StopCoroutine(_invincibilityCoroutine);

        _invincibilityCoroutine = StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        _isInvincible = true;

        float elapsed = 0f;
        bool visible = true;

        while (elapsed < invincibilityDuration)
        {
            visible = !visible;

            SetAlpha(visible ? 1f : blinkAlpha);

            yield return new WaitForSeconds(blinkInterval);

            elapsed += blinkInterval;
        }

        _isInvincible = false;
        _invincibilityCoroutine = null;

        SetAlpha(1f);
    }

    private void StopInvincibility()
    {
        if (_invincibilityCoroutine != null)
        {
            StopCoroutine(_invincibilityCoroutine);
            _invincibilityCoroutine = null;
        }

        _isInvincible = false;
        SetAlpha(1f);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
            return;

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }

    public bool IsInvincible => _isInvincible;
}