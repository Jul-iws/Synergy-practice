using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class Coin : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private GameObject collectEffect;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        var wallet = other.GetComponentInParent<Wallet>();
        if (wallet == null) return;

        wallet.AddCoins(value);
        if (collectEffect != null) Instantiate(collectEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}

