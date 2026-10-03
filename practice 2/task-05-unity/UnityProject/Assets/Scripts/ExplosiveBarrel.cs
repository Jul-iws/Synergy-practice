using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class ExplosiveBarrel : MonoBehaviour
{
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 5f;
    [SerializeField, Min(0.1f)] private float explosionRadius = 4f;
    [SerializeField, Min(1)] private int damage = 40;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private LayerMask affectedLayers = ~0;

    private bool exploded;

    private void OnCollisionEnter(Collision collision)
    {
        if (!exploded && collision.relativeVelocity.magnitude >= minimumImpactSpeed) Explode();
    }

    public void Explode()
    {
        if (exploded) return;
        exploded = true;

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        var damagedTargets = new HashSet<PlayerHealth>();
        foreach (var hit in Physics.OverlapSphere(transform.position, explosionRadius, affectedLayers))
        {
            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null && damagedTargets.Add(health)) health.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

