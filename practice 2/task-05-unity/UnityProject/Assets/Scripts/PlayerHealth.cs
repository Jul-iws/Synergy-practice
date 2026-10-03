using UnityEngine;
using UnityEngine.Events;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private UnityEvent<int, int> healthChanged;
    [SerializeField] private UnityEvent died;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        healthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        healthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth != 0) return;
        died?.Invoke();
        Destroy(gameObject);
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0) return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        healthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}

