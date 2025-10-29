using UnityEngine;

public class TrapGround : MonoBehaviour
{
    [Header("Stat")]
    [SerializeField] int trapDMG = 1;
    [SerializeField] float knockbackForce = 1.0f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();

        if (damageable != null)
        {
            Debug.Log("·Ó´ÒàÁ¨Ç×´æ");
            damageable.TakeDamage(trapDMG, knockbackForce, transform.position);
        }
    }
}
