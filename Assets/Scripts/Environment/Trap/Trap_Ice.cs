using Fusion;
using System.Collections;
using UnityEngine;
using static Unity.Collections.Unicode;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Trap_Ice : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 5f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        if (other.CompareTag("Player"))
        {
            MovementCharacter[] allCharacterMovement = other.GetComponents<MovementCharacter>();

            foreach (var character in allCharacterMovement)
            {
                if (character.enabled)
                {
                    float pushDirectionX = Mathf.Sign(other.transform.position.x - transform.position.x);
                    Vector2 knockbackDirection = new Vector2(pushDirectionX, 1f).normalized;

                    character.TakeDamage(damageAmount, knockbackForce, knockbackDirection);

                    Debug.Log($"Do damage To {character.name}: - {damageAmount} hp");
                }
            }
        }
    }
}
