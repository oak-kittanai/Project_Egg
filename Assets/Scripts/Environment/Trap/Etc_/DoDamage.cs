using UnityEngine;

public class DoDamage : MonoBehaviour
{
    [Header("Stat")]
    [SerializeField] int trapDMG = 1;
    [SerializeField] float knockbackForce = 1.0f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();

        if (damageable != null)
        {
            Vector2 newPos2D = new Vector2(transform.position.x, transform.position.y);
            Debug.Log("trap hit");
            damageable.TakeDamage(trapDMG, knockbackForce, newPos2D);
        }
    }

}
