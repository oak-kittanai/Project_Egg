using Fusion;
using UnityEngine;

public class MovingPlateform : NetworkBehaviour, IRideablePlatform
{
    [SerializeField] float speed = 1.5f;
    [SerializeField] float distance = 4f;
    [SerializeField] bool isVertical;

    private Vector3 startPosition;
    private Rigidbody2D rb;

    // ผิวแพไม่มี friction -> ไม่ลากผู้เล่นซ้อนกับ follow (กันการสไลด์)
    private static PhysicsMaterial2D s_zeroFriction;

    public override void Spawned()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;

        ApplyZeroFriction(GetComponent<Collider2D>());
    }

    private static void ApplyZeroFriction(Collider2D col)
    {
        if (col == null) return;
        if (s_zeroFriction == null)
            s_zeroFriction = new PhysicsMaterial2D("PlatformZeroFriction") { friction = 0f, bounciness = 0f };
        col.sharedMaterial = s_zeroFriction;
    }

    public override void FixedUpdateNetwork()
    {
        // แค่ขยับตัวเอง — ผู้เล่นที่ยืนอยู่บนจะ follow เองผ่าน IRideablePlatform
        // (รับน้ำหนักแนวตั้งจาก kinematic collision, ส่วนแนวนอน player ตามใน MovementCharacter)
        float sineValue = (Mathf.Sin((float)Runner.SimulationTime * speed) + 1f) / 2f;
        float movementOffset = sineValue * distance;

        Vector3 newPosition = startPosition;

        if (isVertical) newPosition.y += movementOffset;
        else newPosition.x += movementOffset;

        rb.MovePosition(newPosition);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
        Vector3 endPosition = currentStart;

        if (isVertical) endPosition.y += distance;
        else endPosition.x += distance;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(currentStart, endPosition);
        Gizmos.DrawSphere(currentStart, 0.1f);
        Gizmos.DrawSphere(endPosition, 0.1f);
    }
}