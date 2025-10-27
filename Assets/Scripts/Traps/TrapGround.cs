using UnityEngine;

public class TrapGround : MonoBehaviour
{
    [Header("Stat")]
    [SerializeField] int trapDMG = 1;
    [SerializeField] float knockbackForce = 1.0f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();

        if (damageable != null)
        {
            Debug.Log("ลองทำดาเมจ");
            damageable.TakeDamage(trapDMG, knockbackForce, collision);
        }
    }
}
