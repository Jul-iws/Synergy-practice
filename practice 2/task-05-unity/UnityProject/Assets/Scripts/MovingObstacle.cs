using UnityEngine;

public sealed class MovingObstacle : MonoBehaviour
{
    [SerializeField] private Vector3 localDirection = Vector3.right;
    [SerializeField, Min(0f)] private float distance = 3f;
    [SerializeField, Min(0f)] private float speed = 2f;
    [SerializeField, Min(0)] private int contactDamage = 20;
    [SerializeField, Min(0f)] private float damageCooldown = 0.75f;

    private Vector3 startPosition;
    private float nextDamageTime;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void FixedUpdate()
    {
        var direction = transform.TransformDirection(localDirection.normalized);
        var offset = Mathf.PingPong(Time.time * speed, distance * 2f) - distance;
        transform.position = startPosition + direction * offset;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Time.time < nextDamageTime) return;

        var health = collision.collider.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        health.TakeDamage(contactDamage);
        nextDamageTime = Time.time + damageCooldown;
    }
}

