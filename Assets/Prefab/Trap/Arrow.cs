using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] int damage = 1;
    [SerializeField] float lifeTime = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage, 0, transform.position);
            Destroy(gameObject);
        }
    }
}
