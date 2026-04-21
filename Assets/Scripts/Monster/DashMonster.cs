using Fusion;
using UnityEngine;

public class DashMonster : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashAcceleration = 50f;
    [SerializeField] private float delayBetweenDashes = 1f;
    [SerializeField] private int maxDashes = 3;

    [Header("Detect")]
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private Vector2 defaultDashDirection = Vector2.right;

    [Header("Damage Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 12f;

    [Header("Networked")]
    [Networked] private int currentDashCount { get; set; }
    [Networked] private Vector2 targetPosition { get; set; }
    [Networked] private NetworkBool isPreparing { get; set; }
    [Networked] private NetworkBool hasSpottedPlayer { get; set; }
    [Networked] private TickTimer actionTimer { get; set; }

    private Rigidbody2D rb2D;

    private void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            currentDashCount = 0;
            hasSpottedPlayer = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!hasSpottedPlayer)
        {
            rb2D.linearVelocity = Vector2.zero;
            if (FindNearestPlayer() != null)
            {
                hasSpottedPlayer = true;
                StartPreparePhase();
            }
            return;
        }

        if (isPreparing)
        {
            rb2D.linearVelocity = Vector2.Lerp(rb2D.linearVelocity, Vector2.zero, Runner.DeltaTime * 10f);

            if (actionTimer.Expired(Runner))
            {
                StartDashPhase();
            }
        }
        else
        {
            Vector2 moveDir = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 targetVelocity = moveDir * dashSpeed;

            rb2D.linearVelocity = Vector2.MoveTowards(rb2D.linearVelocity, targetVelocity, dashAcceleration * Runner.DeltaTime);

            if (Vector2.Distance(transform.position, targetPosition) < 0.6f || actionTimer.Expired(Runner))
            {
                EndDashPhase();
            }
        }
    }
    private void StartPreparePhase()
    {
        isPreparing = true;
        actionTimer = TickTimer.CreateFromSeconds(Runner, delayBetweenDashes);
    }

    private void StartDashPhase()
    {
        isPreparing = false;
        currentDashCount++;

        MovementCharacter targetPlayer = FindNearestPlayer();
        if (targetPlayer != null)
        {
            targetPosition = targetPlayer.transform.position;
        }
        else
        {
            targetPosition = (Vector2)transform.position + (defaultDashDirection.normalized * 7f);
        }

        actionTimer = TickTimer.CreateFromSeconds(Runner, 1.5f); // Failsafe timer
    }

    private void EndDashPhase()
    {
        if (currentDashCount >= maxDashes)
        {
            Runner.Despawn(Object);
        }
        else
        {
            StartPreparePhase();
        }
    }

    private MovementCharacter FindNearestPlayer()
    {
        Collider2D[] hitResults = new Collider2D[10];
        int hitCount = Runner.GetPhysicsScene2D().OverlapCircle(transform.position, detectionRadius, hitResults, targetLayer);

        for (int i = 0; i < hitCount; i++)
        {
            if (hitResults[i].TryGetComponent<MovementCharacter>(out var p) && !p.isDead)
            {
                return p;
            }
        }
        return null;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            if (collision.TryGetComponent<MovementCharacter>(out var player))
            {
                Vector2 knockbackDir = (player.transform.position - transform.position).normalized;

                player.TakeDamage(damage, knockbackForce, knockbackDir);

                Debug.Log($"Monster hit {player.name}! Damage dealt: {damage}");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}