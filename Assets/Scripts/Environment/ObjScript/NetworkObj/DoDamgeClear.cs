using Fusion;
using UnityEngine;

public class DoDamgeClear : NetworkBehaviour
{
    [Header("Trap Setting")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 5f;

    [Header("Lifetime Setting")]
    [SerializeField] private float delayBeforeDes = 0.25f;

    [SerializeField] private float maxLifetime = 6.0f;

    // --- ตัวแปร Network ---
    [Networked] private TickTimer ClearTimer { get; set; }
    [Networked] private TickTimer AutoDestroyTimer { get; set; }
    [Networked] private NetworkBool IsTriggered { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IsTriggered = false;
            ClearTimer = TickTimer.None;

            AutoDestroyTimer = TickTimer.CreateFromSeconds(Runner, maxLifetime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (IsTriggered && ClearTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        if (!IsTriggered && AutoDestroyTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        if (IsTriggered) return;

        if (other.CompareTag("Player"))
        {
            MovementCharacter[] allCharacterMovement = other.GetComponents<MovementCharacter>();

            foreach (var character in allCharacterMovement)
            {
                if (character != null && character.enabled)
                {
                    float pushDirectionX = Mathf.Sign(other.transform.position.x - transform.position.x);
                    Vector2 knockbackDirection = new Vector2(pushDirectionX, 1f).normalized;

                    character.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
                }
            }

            IsTriggered = true;
            ClearTimer = TickTimer.CreateFromSeconds(Runner, delayBeforeDes);
        }
    }
}