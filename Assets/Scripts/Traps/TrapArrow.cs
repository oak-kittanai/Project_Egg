using UnityEngine;

public class TrapArrow : MonoBehaviour
{
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
    [SerializeField] GameObject arrowPrefab; // เปลี่ยนเป็น GameObject ธรรมดา

    private Transform lockedTarget; // ใช้ Transform แทน NetworkObject
    private float fireTimer;

    void Update()
    {
        // 1. ตรวจสอบและหาเป้าหมาย
        ValidateOrFindTarget();

        if (lockedTarget != null)
        {
            // 2. หมุนหน้าตามเป้าหมาย
            RotateTowardsTarget();

            // 3. จับเวลาและยิง
            if (Time.time >= fireTimer)
            {
                if (HasLineOfSight(lockedTarget) && IsInFiringRange())
                {
                    FireArrow();
                    fireTimer = Time.time + fireRate;
                }
            }
        }
    }

    private void ValidateOrFindTarget()
    {
        if (lockedTarget != null)
        {
            float dist = Vector2.Distance(transform.position, lockedTarget.position);
            if (dist > detectRadius || !HasLineOfSight(lockedTarget))
            {
                lockedTarget = null;
            }
        }

        if (lockedTarget == null)
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, detectRadius, playerMask);
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
        Vector2 direction = (Vector2)target.position - (Vector2)transform.position;
        float distance = direction.magnitude;

        // ขยับจุดเริ่ม Raycast ออกมา 0.6 หน่วยเพื่อไม่ให้ชนตัวเอง
        Vector2 startPos = (Vector2)transform.position + ((Vector2)transform.right * 0.6f);
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction.normalized, distance - 0.6f, obstacleMask);

        return hit.collider == null;
    }

    private void RotateTowardsTarget()
    {
        Vector2 direction = (Vector2)lockedTarget.position - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private bool IsInFiringRange()
    {
        Vector2 dirToTarget = ((Vector2)lockedTarget.position - (Vector2)transform.position).normalized;
        return Vector2.Angle(transform.right, dirToTarget) < viewAngle / 2f;
    }

    private void FireArrow()
    {
        if (arrowPrefab == null) return;
        // ใช้ Instantiate ปกติ
        Instantiate(arrowPrefab, transform.position + (transform.right * 0.5f), transform.rotation);
    }

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