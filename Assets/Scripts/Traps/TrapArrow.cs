using Fusion;
using UnityEngine;

public class TrapArrow : NetworkBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] float detectRadius = 5f;   
    [Range(0, 360)]
    [SerializeField] float viewAngle = 45f; 

    [Header("Firing Settings")]
    [SerializeField] float fireRate = 2.5f;
    [SerializeField] float arrowSpeed = 10f;
    [Range(0, 90)]
    [SerializeField] float launchAngle = 45f; 

    [Header("References")]
    [SerializeField] LayerMask playerMask;
    [NetworkPrefab] public NetworkObject arrowPrefab;

    float fireTime;

    private void Update()
    {
        detectFire();
    }

    void detectFire()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius, playerMask);

        if (hits.Length == 0) return;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var idamage))
            {
                Vector2 dirToTarget = (hit.transform.position - transform.position).normalized;
                if (Vector2.Angle(transform.right, dirToTarget) < viewAngle / 2f)
                {
                    if (Time.time >= fireTime + fireRate)
                    {
                        //FireArrow(hit.transform);
                        fireTime = Time.time;
                    }
                }
            }
        }
    }

    public void FireArrow(Transform target)
    {
        if (Object.HasStateAuthority == false) return;

        Vector2 direction = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        GameManager.Instance.ProjectileSpawn(arrowPrefab, transform.position, direction, rotation);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        Vector3 rightDir = transform.right;
        Vector3 upperBoundary = Quaternion.Euler(0, 0, viewAngle / 2f) * rightDir;
        Vector3 lowerBoundary = Quaternion.Euler(0, 0, -viewAngle / 2f) * rightDir;

        Gizmos.DrawLine(transform.position, transform.position + upperBoundary * detectRadius);
        Gizmos.DrawLine(transform.position, transform.position + lowerBoundary * detectRadius);
    }
}