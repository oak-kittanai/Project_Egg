using Fusion;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class DoDamage : NetworkBehaviour
{
    [Header("Trap Setting")]
    //[SerializeField] private float cooldownTime = 1.5f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        MovementCharacter[] allCharacterMovement = other.GetComponents<MovementCharacter>();

        foreach (var character in allCharacterMovement)
        {
            if (character.enabled)
            {
                if (HasStateAuthority)
                {
                    if (other.CompareTag("Player"))
                    {
                        if (other.TryGetComponent<IDamageable>(out var damageable))
                        {
                            float pushDirectionX = Mathf.Sign(other.transform.position.x - transform.position.x);
                            Vector2 knockbackDirection = new Vector2(pushDirectionX, 1f).normalized;

                            character.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
                        }
                    }

                    Debug.Log($"Do damage To {character.name}: - {damageAmount} hp");
                }
            }
        }
    }

}
