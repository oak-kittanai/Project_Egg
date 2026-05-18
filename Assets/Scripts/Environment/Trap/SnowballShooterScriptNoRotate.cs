using Fusion;
using UnityEngine;

public class Turret_Shooter : NetworkBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 6f;
    public float viewAngle = 22.5f;
    public LayerMask playerLayer;

    [Header("Shooting Settings")]
    public NetworkObject bulletPrefab;
    [SerializeField] float projectileSpeed = 10f;

    public float fireRate = 1.5f;

    public Transform firePoint;

    [Header("Audio Setting")]
    public AudioSource shootAudioSource;
    public AudioClip shootSoundClip;

    [Networked] private TickTimer FireTimer { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayer);

        Transform targetToShoot = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<MovementCharacter>(out _)) continue;

            Vector2 dirToTarget = (hit.transform.position - transform.position).normalized;
            float distance = Vector2.Distance(transform.position, hit.transform.position);

            if (Vector2.Angle(transform.right, dirToTarget) <= viewAngle)
            {
                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetToShoot = hit.transform;
                }
            }
        }

        if (targetToShoot != null && FireTimer.ExpiredOrNotRunning(Runner))
        {
            ShootAt(targetToShoot);
            FireTimer = TickTimer.CreateFromSeconds(Runner, fireRate);
        }
    }

    private void ShootAt(Transform target)
    {
        Vector2 spawnPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        Vector2 direction = ((Vector2)target.position - spawnPos).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0, 0, angle);

        if (GameManager.Instance != null && bulletPrefab != null)
        {
            GameManager.Instance.ProjectileSpawn(bulletPrefab, spawnPos, direction, bulletRotation, projectileSpeed);

            RPC_PlayShootSound();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayShootSound()
    {
        if (shootAudioSource != null && shootSoundClip != null)
        {
            shootAudioSource.PlayOneShot(shootSoundClip);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Vector3 upLimit = Quaternion.Euler(0, 0, viewAngle) * transform.right * detectionRadius;
        Vector3 downLimit = Quaternion.Euler(0, 0, -viewAngle) * transform.right * detectionRadius;
        Gizmos.DrawLine(transform.position, transform.position + upLimit);
        Gizmos.DrawLine(transform.position, transform.position + downLimit);
    }
}