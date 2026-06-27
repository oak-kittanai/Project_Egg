using Fusion;
using UnityEngine;

public class SnowBallProjectile : NetworkBehaviour
{
    [Header("Movement Setting")]
    [SerializeField] private float speed = 15f;

    [Header("Snowball Setting")]
    [SerializeField] private float minBounceForce = 2f;
    [Tooltip("ถ้าความเร็วของหิมะต่ำกว่านี้ (เช่นกลิ้งช้าๆบนพื้น) จะไม่ทำดาเมจผู้เล่นอีก")]
    [SerializeField] private float minSpeedToDamage = 3f;

    [Header("Damage Setting")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 4f;

    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float hitRadius = 0.5f;

    [Networked] private TickTimer LifeTimer { get; set; }
    private bool hasHitPlayer = false;
    private Rigidbody2D rb2D;

    // freeze ตอน pause: เก็บค่าความเร็ว/แรงโน้มถ่วงไว้ แล้วหยุดนิ่ง คืนค่าเมื่อเล่นต่อ
    private bool _frozen;
    private Vector2 _frozenVel;
    private float _frozenAng;
    private float _frozenGravity;

    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);

        rb2D = GetComponent<Rigidbody2D>();

        if (HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, 7f);
            hasHitPlayer = false;

            if (rb2D != null)
            {
                rb2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb2D.linearVelocity = transform.right * speed;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen)
        {
            if (!_frozen && rb2D != null)
            {
                _frozenVel = rb2D.linearVelocity;
                _frozenAng = rb2D.angularVelocity;
                _frozenGravity = rb2D.gravityScale;
                rb2D.linearVelocity = Vector2.zero;
                rb2D.angularVelocity = 0f;
                rb2D.gravityScale = 0f;
                _frozen = true;
            }
            return;
        }
        if (_frozen && rb2D != null)
        {
            rb2D.linearVelocity = _frozenVel;
            rb2D.angularVelocity = _frozenAng;
            rb2D.gravityScale = _frozenGravity;
            _frozen = false;
        }

        if (LifeTimer.Expired(Runner))
        {
            DespawnSnowball();
            return;
        }

        bool isMovingFastEnough = rb2D != null && rb2D.linearVelocity.magnitude >= minSpeedToDamage;

        if (!hasHitPlayer && isMovingFastEnough)
        {
            MovementCharacter[] allPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);

            foreach (MovementCharacter character in allPlayers)
            {

                if (character == null || character.isDead) continue;

                float distance = Vector2.Distance(transform.position, character.transform.position);

                if (distance <= hitRadius)
                {
                    hasHitPlayer = true;

                    Vector2 knockbackDir = (character.transform.position - transform.position).normalized;
                    knockbackDir.y = 1f;

                    character.TakeDamage(damageAmount, knockbackForce, knockbackDir.normalized);

                    DespawnSnowball();
                    break;
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