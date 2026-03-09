using Fusion;
using UnityEngine;

public class SnowBallProjectile : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 15f;

    [Header("Snowball Settings")]
    [SerializeField] private float minBounceForce = 2f;

    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 4f;
    [Networked] private TickTimer LifeTimer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, 7f);

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = transform.right * speed;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (LifeTimer.Expired(Runner))
        {
            DespawnSnowball();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                knockbackDir.y = 1f;
                damageable.TakeDamage(damageAmount, knockbackForce, knockbackDir.normalized);
            }
            DespawnSnowball();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        int layer = collision.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Ground") || layer == LayerMask.NameToLayer("Platform"))
        {
            float impactY = Mathf.Abs(collision.relativeVelocity.y);

            if (impactY < minBounceForce)
            {
                DespawnSnowball();
            }
        }
    }

    private void DespawnSnowball()
    {
        if (GameManager.Instance != null && Object != null && Object.IsValid)
        {
            GameManager.Instance.RequestDespawn(this.Object);
        }
    }
}