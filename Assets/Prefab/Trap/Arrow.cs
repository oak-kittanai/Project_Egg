using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] float speed = 15f;
    [SerializeField] int damage = 1;
    [SerializeField] float lifeTime = 5f;
    [SerializeField] LayerMask obstacleMask;

    void Start()
    {
        // ใช้ Destroy ของ Unity ปกติ
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // เคลื่อนที่ไปข้างหน้า
        transform.position += transform.right * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // เช็คชน Player
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(damage, 0, transform.position);
            }
            Destroy(gameObject);
        }
        // เช็คชนกำแพง
        else if (((1 << collision.gameObject.layer) & obstacleMask) != 0)
        {
            Destroy(gameObject);
        }
    }
}