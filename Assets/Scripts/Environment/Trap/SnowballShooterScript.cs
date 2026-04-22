using Fusion;
using UnityEngine;

public class SnowballShooterTrap : NetworkBehaviour
{
    [Networked] public NetworkBool IsActive { get; set; }

    [Header("Visuals & Parts")]
    [SerializeField] Transform gunBarrel;

    [Header("Detection Settings")]
    [SerializeField] float detectRadius = 5f;
    [Range(0, 360)]
    [SerializeField] float viewAngle = 45f;
    [SerializeField] LayerMask playerMask;
    [SerializeField] LayerMask obstacleMask;

    [Header("Rotation Settings")]
    [SerializeField] float rotationSpeed = 5f;

    [Header("Firing Settings")]
    [SerializeField] float fireRate = 2.5f;
    [SerializeField] float projectileSpeed = 10f;

    [SerializeField] NetworkObject snowballPrefab;

    private Transform lockedTarget;

    [Networked] private TickTimer FireTimer { get; set; }
    [Networked] private float NetTargetAngle { get; set; }

    private Transform Pivot => gunBarrel != null ? gunBarrel : transform;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IsActive = true;
            NetTargetAngle = Pivot.eulerAngles.z;
            FireTimer = TickTimer.CreateFromSeconds(Runner, fireRate);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsActive) return;

        ValidateOrFindTarget();

        if (lockedTarget != null)
        {
            Vector2 direction = (Vector2)lockedTarget.position - (Vector2)Pivot.position;
            NetTargetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

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

    public override void Render()
    {
        Quaternion targetRotation = Quaternion.Euler(0, 0, NetTargetAngle);
        Pivot.rotation = Quaternion.Lerp(Pivot.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ValidateOrFindTarget()
    {
        if (lockedTarget != null)
        {
            float dist = Vector2.Distance(Pivot.position, lockedTarget.position);
            if (dist > detectRadius || !HasLineOfSight(lockedTarget))
            {
                lockedTarget = null;
            }
        }

        if (lockedTarget == null)
        {
            Collider2D hit = Physics2D.OverlapCircle(Pivot.position, detectRadius, playerMask);
            if (hit != null)
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
        Vector2 direction = (Vector2)target.position - (Vector2)Pivot.position;
        float distance = direction.magnitude;

        Vector2 startPos = (Vector2)Pivot.position + ((Vector2)Pivot.right * 0.6f);
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction.normalized, distance - 0.6f, obstacleMask);

        return hit.collider == null;
    }

    private bool IsInFiringRange()
    {
        Vector2 dirToTarget = ((Vector2)lockedTarget.position - (Vector2)Pivot.position).normalized;
        return Vector2.Angle(Pivot.right, dirToTarget) < viewAngle / 2f;
    }

    private void FireSnowball()
    {
        if (snowballPrefab == null) return;

        Vector2 spawnPos = (Vector2)Pivot.position + ((Vector2)Pivot.right * 0.5f);
        Vector2 fireDirection = Pivot.right;

        GameManager.Instance.ProjectileSpawn(snowballPrefab, spawnPos, fireDirection, Pivot.rotation, projectileSpeed);
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
        Gizmos.DrawWireSphere(Pivot.position, detectRadius);
        Vector3 rightDir = Pivot.right;
        Vector3 upper = Quaternion.Euler(0, 0, viewAngle / 2f) * rightDir;
        Vector3 lower = Quaternion.Euler(0, 0, -viewAngle / 2f) * rightDir;
        Gizmos.DrawLine(Pivot.position, Pivot.position + upper * detectRadius);
        Gizmos.DrawLine(Pivot.position, Pivot.position + lower * detectRadius);
    }
}