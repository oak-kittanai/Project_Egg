using Fusion;
using UnityEngine;

public class RollingLogTrap : NetworkBehaviour
{
    [Header("Waypoints")]
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

    private Rigidbody2D rb;
    private Vector2 p1Pos;
    private Vector2 p2Pos;

    private void Awake()
    {
        if (point1 != null) point1.SetParent(null);
        if (point2 != null) point2.SetParent(null);
    }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();

        if (HasStateAuthority)
        {
            CurrentPosition = transform.position;
            CurrentRotation = transform.eulerAngles.z;
            MovingToPoint2 = true;
            p1Pos = point1 != null ? point1.position : transform.position;
            p2Pos = point2 != null ? point2.position : transform.position;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen) return; // freeze ตอน pause/dialogue/tutorial

        Vector2 targetPos = MovingToPoint2 ? p2Pos : p1Pos;
        CurrentPosition = Vector2.MoveTowards(CurrentPosition, targetPos, moveSpeed * Runner.DeltaTime);
        float moveDirX = 0f;

        if (Mathf.Abs(targetPos.x - CurrentPosition.x) > 0.01f)
        {
            moveDirX = Mathf.Sign(targetPos.x - CurrentPosition.x);
        }

        CurrentRotation -= moveDirX * rotationSpeed * Runner.DeltaTime;

        if (Vector2.Distance(CurrentPosition, targetPos) < 0.05f)
        {
            MovingToPoint2 = !MovingToPoint2;
        }

        if (rb != null)
        {
            rb.position = CurrentPosition;
            rb.rotation = CurrentRotation;
        }
        else
        {
            transform.position = CurrentPosition;
            transform.rotation = Quaternion.Euler(0, 0, CurrentRotation);
        }
    }

    public override void Render()
    {
        if (!HasStateAuthority)
        {
            transform.position = Vector2.Lerp(transform.position, CurrentPosition, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, CurrentRotation), Time.deltaTime * 15f);
        }
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
                    if (pushDirectionX == 0) pushDirectionX = 1f;

                    Vector2 knockbackDirection = new Vector2(pushDirectionX, 1f).normalized;

                    character.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
                }
            }
        }
    }
}