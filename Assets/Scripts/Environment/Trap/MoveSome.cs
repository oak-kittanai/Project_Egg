using Fusion;
using UnityEngine;

// ของที่เลื่อนไปกลับตามเวลา (แมงกะพรุน JellyTrap) — ตำแหน่งเป็นฟังก์ชันของ tick ทุกเครื่องคำนวณเองแบบ MovingPlateform (P-3)
// เดิม host คำนวณคนเดียวแล้ว sync ตำแหน่ง ฝั่ง client แค่ lerp ตามค่าล่าสุด -> จอ client เห็นแมงกะพรุนช้ากว่าจริง
// ระเบิด (ตัดสินบน host) จึงไม่ตรงกับที่เห็น
public class MoveSome : NetworkBehaviour, IRideablePlatform, IBeforeTick
{
    [Header("Move State")]
    [SerializeField] float speed = 1.5f;
    [SerializeField] float distance = 4f;
    [SerializeField] bool isVertical;
    [SerializeField] bool isReverse;

    // จุดที่วางไว้ในฉาก (host ส่งให้ทุกเครื่อง) และ tick ที่สั่งหยุด (0 = กำลังเคลื่อนที่)
    [Networked] private Vector3 StartPosition { get; set; }
    [Networked] private int HoldTick { get; set; }

    // host: ขอให้หยุดอยู่กับที่ (JellyfishTrap ตั้งตอนชาร์จ/ระเบิด/ซ่อน)
    public bool HoldRequested { get; set; }

    private Vector3 _authoredPosition;
    private Vector3 startPosition;
    private Rigidbody2D rb;

    // ผิวแพไม่มี friction -> ไม่ลากผู้เล่นซ้อนกับ follow (กันการสไลด์)
    private static PhysicsMaterial2D s_zeroFriction;

    private void Awake()
    {
        _authoredPosition = transform.position;
    }

    public override void Spawned()
    {
        // client คำนวณตำแหน่งเองด้วย
        Runner.SetIsSimulated(Object, true);

        if (HasStateAuthority)
        {
            StartPosition = _authoredPosition;
            HoldTick = 0;
        }
        startPosition = StartPosition;

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
    }

    // หยุดอยู่: อยู่ที่ตำแหน่งของ tick ที่หยุด; เคลื่อนที่ต่อ: กลับไปตำแหน่งตามเวลา (เหมือนเดิมที่ใช้ SimulationTime)
    private Vector3 PositionAt(float tick)
    {
        float time = (HoldTick != 0 ? HoldTick : tick) * Runner.DeltaTime;
        float sineValue = (Mathf.Sin(time * speed) + 1f) / 2f;
        float directionMultiplier = isReverse ? -1f : 1f;
        float movementOffset = sineValue * distance * directionMultiplier;

        Vector3 position = startPosition;

        if (isVertical) position.y += movementOffset;
        else position.x += movementOffset;

        return position;
    }

    // ต้น tick (รวมตอน resimulate): วางที่ตำแหน่งปลาย tick ก่อนหน้า ทุกเครื่องจึงเริ่มจากจุดเดียวกัน
    void IBeforeTick.BeforeTick()
    {
        if (rb == null) return;

        Vector3 position = PositionAt(Runner.Tick.Raw - 1);
        transform.position = position;
        rb.position = position;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // freeze ตอน pause/dialogue/tutorial หรือ JellyfishTrap ขอหยุด
            bool hold = HoldRequested || (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen);
            if (hold && HoldTick == 0) HoldTick = Runner.Tick.Raw;
            else if (!hold && HoldTick != 0) HoldTick = 0;
        }

        if (rb != null)
        {
            rb.MovePosition(PositionAt(Runner.Tick.Raw));
        }
    }

    // วาดระหว่าง tick ก่อนหน้ากับ tick ล่าสุด — จังหวะเดียวกับตัวผู้เล่นที่ทำนายอยู่ในเครื่องนี้
    public override void Render()
    {
        transform.position = PositionAt(Runner.Tick.Raw - 1 + Runner.LocalAlpha);
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
