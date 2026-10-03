using TMPro;
using UnityEngine;

public sealed class Wallet : MonoBehaviour
{
    [SerializeField] private TMP_Text counter;

    public int Coins { get; private set; }

    private void Start()
    {
        RefreshCounter();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        Coins += amount;
        RefreshCounter();
    }

    private void RefreshCounter()
    {
        if (counter != null) counter.text = $"Монеты: {Coins}";
    }
}

