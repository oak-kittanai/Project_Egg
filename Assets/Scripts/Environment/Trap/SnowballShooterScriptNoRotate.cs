using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Turret_Shooter : NetworkBehaviour
{
    [Header("Detect")]
    public float detectionRadius = 6f;
    public float viewAngle = 22.5f;
    public LayerMask playerLayer;

    [Header("กันยิงทะลุ")]
    public bool checkLineOfSight = true;
    public LayerMask obstacleLayer;

    [Header("Shooting Setting")]
    public NetworkObject bulletPrefab;
    [SerializeField] float projectileSpeed = 10f;

    public float fireRate = 1.5f;
    public Transform firePoint;

    [Header("Audio")]
    public AudioSource shootAudioSource;
    public AudioClip shootSoundClip;

    [Networked] private TickTimer FireTimer { get; set; }

    private readonly HashSet<Collider2D> collidersInRange = new HashSet<Collider2D>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        collidersInRange.Add(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        collidersInRange.Remove(other);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen) return; // freeze ตอน pause/dialogue/tutorial

        Vector2 originPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        Vector2? targetPosToShoot = null;
        float minDistance = float.MaxValue;

        foreach (var hit in collidersInRange)
        {
            if (hit == null) continue;

            MovementCharacter character = hit.GetComponentInParent<MovementCharacter>();
            if (character == null) character = hit.GetComponentInChildren<MovementCharacter>();

            if (character == null || character.isDead) continue;

            Vector2 targetPos = hit.bounds.center;
            Vector2 dirToTarget = (targetPos - originPos).normalized;
            float distance = Vector2.Distance(originPos, targetPos);

            if (Vector2.Angle(transform.right, dirToTarget) <= viewAngle)
            {
                bool canSeePlayer = true;

                if (checkLineOfSight)
                {
                    RaycastHit2D hitObstacle = Physics2D.Raycast(originPos, dirToTarget, distance, obstacleLayer);
                    if (hitObstacle.collider != null)
                    {
                        canSeePlayer = false;
                    }
                }

                if (canSeePlayer && distance < minDistance)
                {
                    minDistance = distance;
                    targetPosToShoot = targetPos;
                }
            }
        }

        if (targetPosToShoot != null && FireTimer.ExpiredOrNotRunning(Runner))
        {
            ShootAt(targetPosToShoot.Value);
            FireTimer = TickTimer.CreateFromSeconds(Runner, fireRate);
        }
    }

    private void ShootAt(Vector2 targetPos)
    {
        Vector2 spawnPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 direction = (targetPos - spawnPos).normalized;
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
        Vector2 originPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(originPos, detectionRadius);
        Vector3 upLimit = Quaternion.Euler(0, 0, viewAngle) * transform.right * detectionRadius;
        Vector3 downLimit = Quaternion.Euler(0, 0, -viewAngle) * transform.right * detectionRadius;
        Gizmos.DrawLine(originPos, new Vector3(originPos.x, originPos.y, 0) + upLimit);
        Gizmos.DrawLine(originPos, new Vector3(originPos.x, originPos.y, 0) + downLimit);
    }
}