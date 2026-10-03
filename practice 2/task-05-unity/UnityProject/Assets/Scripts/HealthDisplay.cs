using TMPro;
using UnityEngine;

public sealed class HealthDisplay : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TMP_Text label;

    private void Update()
    {
        if (label == null) return;

        label.text = playerHealth == null
            ? "Здоровье: 0"
            : $"Здоровье: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}";
    }
}

