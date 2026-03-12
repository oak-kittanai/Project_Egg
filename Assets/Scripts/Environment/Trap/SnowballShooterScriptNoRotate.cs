using Fusion;
using UnityEngine;

public class SnowballShooterTrapNoRotate : NetworkBehaviour
{
    [Networked] public NetworkBool IsActive { get; set; }

    [Header("Detection Settings")]
    [SerializeField] float detectRadius = 5f;
    [Range(0, 360)]
    [SerializeField] float viewAngle = 45f;
    [SerializeField] LayerMask playerMask;
    [SerializeField] LayerMask obstacleMask;

    [Header("Firing Settings")]
    [SerializeField] float fireRate = 2.5f;

    [SerializeField] NetworkObject snowballPrefab;

    private Transform lockedTarget;

    [Networked] private TickTimer FireTimer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IsActive = true;
            FireTimer = TickTimer.CreateFromSeconds(Runner, fireRate);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsActive) return;

        ValidateOrFindTarget();

        if (lockedTarget != null)
        {
            if (FireTimer.ExpiredOrNotRunning(Runner))
            {
                if (HasLineOfSight(lockedTarget) && IsInFiringRange())
                {
                    FireSnowball();
                    FireTimer = TickTimer.CreateFromSeconds(Runner, fireRate);
                }
            }
        }
    }

    private void ValidateOrFindTarget()
    {
        if (lockedTarget != null)
        {
            float dist = Vector2.Distance(transform.position, lockedTarget.position); if (dist > detectRadius || !HasLineOfSight(lockedTarget))
            {
                lockedTarget = null;
            }
        }

        if (lockedTarget == null)
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, detectRadius, playerMask); if (hit != null)
            {
                if (HasLineOfSight(hit.transform))
                {
                    lockedTarget = hit.transform;
                }
            }
        }
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector2 direction = (Vector2)target.position - (Vector2)transform.position;
        float distance = direction.magnitude;

        Vector2 startPos = (Vector2)transform.position + ((Vector2)transform.right * 0.6f);
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction.normalized, distance - 0.6f, obstacleMask);

        return hit.collider == null;
    }

    private bool IsInFiringRange()
    {
        Vector2 dirToTarget = ((Vector2)lockedTarget.position - (Vector2)transform.position).normalized;
        return Vector2.Angle(transform.right, dirToTarget) < viewAngle / 2f;
    }

    private void FireSnowball()
    {
        if (snowballPrefab == null) return;

        Vector2 spawnPos = (Vector2)transform.position + ((Vector2)transform.right * 0.5f);
        Vector2 fireDirection = transform.right;

        GameManager.Instance.ProjectileSpawn(snowballPrefab, spawnPos, fireDirection, transform.rotation);
    }

    public void SetTrapActive(bool active)
    {
        if (HasStateAuthority) IsActive = active;
        else RPC_SetTrapActive(active);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetTrapActive(NetworkBool active) => IsActive = active;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        Vector3 rightDir = transform.right;
        Vector3 upper = Quaternion.Euler(0, 0, viewAngle / 2f) * rightDir;
        Vector3 lower = Quaternion.Euler(0, 0, -viewAngle / 2f) * rightDir;
        Gizmos.DrawLine(transform.position, transform.position + upper * detectRadius);
        Gizmos.DrawLine(transform.position, transform.position + lower * detectRadius);
    }
}