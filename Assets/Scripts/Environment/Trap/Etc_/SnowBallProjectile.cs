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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        MovementCharacter[] characters = collision.gameObject.GetComponentsInParent<MovementCharacter>();

        if (characters == null || characters.Length == 0)
        {
            characters = collision.gameObject.GetComponentsInChildren<MovementCharacter>();
        }

        bool hitPlayer = false;

        if (characters != null && characters.Length > 0)
        {
            foreach (var character in characters)
            {

                if (character.enabled)
                {
                    Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                    knockbackDir.y = 1f;

                    character.TakeDamage(damageAmount, knockbackForce, knockbackDir.normalized);

                    DespawnSnowball();
                    hitPlayer = true;
                    return; 
                }
            }
        }

        if (!hitPlayer)
        {
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
    }

    private void DespawnSnowball()
    {
        if (GameManager.Instance != null && Object != null && Object.IsValid)
        {
            GameManager.Instance.RequestDespawn(this.Object);
        }
    }
}