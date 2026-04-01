using Fusion;
using UnityEngine;

public class Trap_Ice : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 5f;

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            MovementCharacter[] allCharacterMovement = collision.gameObject.GetComponents<MovementCharacter>();

            foreach (var character in allCharacterMovement)
            {
                if (character.enabled)
                {
                    float pushDirectionX = Mathf.Sign(collision.transform.position.x - transform.position.x);
                    Vector2 knockbackDirection = new Vector2(pushDirectionX, 1f).normalized;

                    character.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
                }
            }
        }
    }
}