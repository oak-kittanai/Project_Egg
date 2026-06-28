using Fusion;
using UnityEngine;

public class MoveSome : NetworkBehaviour, IRideablePlatform
{
    [Header("Move State")]
    [SerializeField] float speed = 1.5f;
    [SerializeField] float distance = 4f;
    [SerializeField] bool isVertical;
    [SerializeField] bool isReverse;
    [Networked] private Vector3 CurrentPosition { get; set; }

    private Vector3 startPosition;
    private Rigidbody2D rb;

    // ผิวแพไม่มี friction -> ไม่ลากผู้เล่นซ้อนกับ follow (กันการสไลด์)
    private static PhysicsMaterial2D s_zeroFriction;

    public override void Spawned()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;
        }

        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (s_zeroFriction == null)
                s_zeroFriction = new PhysicsMaterial2D("PlatformZeroFriction") { friction = 0f, bounciness = 0f };
            col.sharedMaterial = s_zeroFriction;
        }

        if (HasStateAuthority)
        {
            CurrentPosition = startPosition;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen) return; // freeze ตอน pause/dialogue/tutorial

        if (HasStateAuthority)
        {
            float sineValue = (Mathf.Sin((float)Runner.SimulationTime * speed) + 1f) / 2f;
            float directionMultiplier = isReverse ? -1f : 1f;
            float movementOffset = sineValue * distance * directionMultiplier;

            Vector3 newPosition = startPosition;

            if (isVertical)
            {
                newPosition.y += movementOffset;
            }
            else
            {
                newPosition.x += movementOffset;
            }

            CurrentPosition = newPosition;
        }

        if (rb != null)
        {
            rb.MovePosition(CurrentPosition);
        }
    }

    public override void Render()
    {
        transform.position = Vector3.Lerp(transform.position, CurrentPosition, Runner.DeltaTime * 15f);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
        Vector3 endPosition = currentStart;

        float directionMultiplier = isReverse ? -1f : 1f;

        if (isVertical)
        {
            endPosition.y += distance * directionMultiplier;
        }
        else
        {
            endPosition.x += distance * directionMultiplier;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(currentStart, endPosition);
        Gizmos.DrawSphere(currentStart, 0.1f);
        Gizmos.DrawSphere(endPosition, 0.1f);
    }
}