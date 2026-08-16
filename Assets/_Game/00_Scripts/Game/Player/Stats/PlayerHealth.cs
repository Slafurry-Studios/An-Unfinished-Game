using System;
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

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (debug &&Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (currentHealth <= 0)
        {
            Die();
        }

        OnTakeDamage?.Invoke();
    }

    private void Die()
    {
        IsDead = true;
        Debug.Log("Player has died.");
        OnDied?.Invoke();
    }
}