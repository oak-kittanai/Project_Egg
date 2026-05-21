using Fusion;
using UnityEngine;

public class SnowBallProjectile : NetworkBehaviour
{
    [Header("Movement Setting")]
    [SerializeField] private float speed = 15f;

    [Header("Snowball Setting")]
    [SerializeField] private float minBounceForce = 2f;

    [Header("Damage Setting")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 4f;

    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float hitRadius = 0.5f;

    [Networked] private TickTimer LifeTimer { get; set; }
    private bool hasHitPlayer = false;

    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);

        if (HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, 7f);
            hasHitPlayer = false;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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
            return;
        }

        if (!hasHitPlayer)
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, hitRadius, playerLayer);
            if (hit != null)
            {
                MovementCharacter character = hit.GetComponentInParent<MovementCharacter>();
                if (character == null) character = hit.GetComponentInChildren<MovementCharacter>();

                if (character != null && character.enabled && !character.isDead)
                {
                    hasHitPlayer = true;
                    Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                    knockbackDir.y = 1f;

                    character.TakeDamage(damageAmount, knockbackForce, knockbackDir.normalized);

                    DespawnSnowball();
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority || hasHitPlayer) return;

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}