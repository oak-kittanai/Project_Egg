using Fusion;
using UnityEngine;

public class Arrow : NetworkBehaviour
{
    [SerializeField] NetworkObject selfObj;
    [SerializeField] int damage = 1;
    [SerializeField] float lifeTime = 5f;

    private void Awake()
    {
        selfObj = GetComponent<NetworkObject>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage, 0, transform.position);
            GameManager.Instance.RequestDespawn(selfObj);
        }
    }
}
