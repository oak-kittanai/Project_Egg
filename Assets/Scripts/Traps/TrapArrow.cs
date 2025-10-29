using UnityEngine;

public class TrapArrow : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] float detectRadiuse = 3f;
    [SerializeField] float fireRate = 2.5f;
    [SerializeField] float arrowSpeed = 8f;
    [SerializeField] LayerMask playerMask;
    [SerializeField] GameObject arrowPrefabe;

    float fireTime; // เวลายิงล่าสุด

    private void Update()
    {
        detectFire();
    }

    void detectFire()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadiuse, playerMask);

        if (hits.Length == 0) return; // ไม่เจอใครก็หยุด

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var idamage))
            {
                if (Time.time >= fireTime + fireRate)
                {
                    fireArrow(hit.transform);
                    fireTime = Time.time; // อัพเดตเวลายิงล่าสุด
                }
            }
        }   
    }
    void fireArrow(Transform target)
    {
        // ไม่เจอ Prefab ของธนู
        if (arrowPrefabe == null) return;

        GameObject arrow = Instantiate(arrowPrefabe, transform.position, Quaternion.identity);

        Vector2 dir = (target.position - transform.position);

        Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();
        rb.AddForce(dir * arrowSpeed, ForceMode2D.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadiuse);
    }

}
