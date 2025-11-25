using UnityEngine;

public class TrapArrow : MonoBehaviour
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
    [SerializeField] GameObject arrowPrefabe;

    float fireTime;

    private void Update()
    {
        detectFire();
    }

    void detectFire()
    {
        // 1. หาคนในระยะวงกลมก่อน (ประหยัด Performance)
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
                        fireArrow(hit.transform);
                        fireTime = Time.time;
                    }
                }
            }
        }
    }

    void fireArrow(Transform target)
    {
        if (arrowPrefabe == null) return;

        GameObject arrow = Instantiate(arrowPrefabe, transform.position, Quaternion.identity);
        Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();

        // 1. หาบทิศทางตรงๆ หาเป้าหมาย (Target - Start)
        Vector2 direction = (target.position - transform.position).normalized;

        // 2. หมุนหัวลูกธนูไปหาเป้าหมาย
        // ใช้ Atan2 เพื่อหามุมจาก Vector
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 3. ยิงออกไปตรงๆ
        // แนะนำให้ใช้ velocity เพื่อความเร็วที่คงที่และพุ่งตรงทันที
        rb.velocity = direction * arrowSpeed;
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