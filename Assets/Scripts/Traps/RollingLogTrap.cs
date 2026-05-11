using Fusion;
using UnityEngine;

public class RollingLogTrap : NetworkBehaviour
{
    [Header("Point Setting")]
    [SerializeField] private Transform point1;
    [SerializeField] private Transform point2;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Damage Setting")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 5f;

    [Networked] private Vector2 CurrentPosition { get; set; }
    [Networked] private float CurrentRotation { get; set; }
    [Networked] private NetworkBool MovingToPoint2 { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentPosition = transform.position;
            CurrentRotation = transform.eulerAngles.z;
            MovingToPoint2 = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        Vector2 targetPos = MovingToPoint2 ? point2.position : point1.position;

        CurrentPosition = Vector2.MoveTowards(CurrentPosition, targetPos, moveSpeed * Runner.DeltaTime);

        float moveDirX = Mathf.Sign(targetPos.x - CurrentPosition.x);
        CurrentRotation -= moveDirX * rotationSpeed * Runner.DeltaTime;

        if (Vector2.Distance(CurrentPosition, targetPos) < 0.05f)
        {
            MovingToPoint2 = !MovingToPoint2;
        }
    }

    public override void Render()
    {
        transform.position = Vector2.Lerp(transform.position, CurrentPosition, Runner.DeltaTime * 15f);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, CurrentRotation), Runner.DeltaTime * 15f);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (collision.CompareTag("Player"))
        {
            MovementCharacter[] allCharacterMovement = collision.GetComponents<MovementCharacter>();

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