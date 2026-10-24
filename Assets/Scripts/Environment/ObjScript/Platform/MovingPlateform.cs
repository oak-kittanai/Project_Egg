using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

// แพเลื่อนไปกลับตามเวลา — ตำแหน่งเป็นฟังก์ชันของ tick ล้วน ๆ ทุกเครื่องคำนวณเองได้ตรงกันโดยไม่ต้อง sync (NR-10)
// เดิม NetworkRigidbody2D ใน prefab ฝั่ง client ทับตำแหน่ง root ด้วยค่าจาก host ตอน resimulate และเขียน root อีกรอบตอน Render
// -> สองแหล่งเขียนตำแหน่งเดียวกันคนละจังหวะ แพวาร์ปไปมาบนจอ client แม้ไม่มีใครยืน
public class MovingPlateform : NetworkBehaviour, IRideablePlatform, IBeforeTick
{
    [SerializeField] float speed = 1.5f;
    [SerializeField] float distance = 4f;
    [SerializeField] bool isVertical;

    // จุดตั้งต้นที่วางไว้ในฉาก — host ส่งให้ทุกเครื่อง (P-1)
    // เดิมแต่ละเครื่องอ่าน transform ตอน Spawned แต่ฝั่ง client NetworkRigidbody2D ย้าย transform ไปตำแหน่งปัจจุบันของ host ไว้แล้ว
    // -> แพของ client แกว่งรอบจุดที่เลื่อนไป: แต่ละคนยืนบนแพของตัวเองได้ แต่อีกจอเห็นลอย
    [Networked] private Vector3 StartPosition { get; set; }

    private Vector3 _authoredPosition;
    private Vector3 startPosition;
    private Rigidbody2D rb;

    // ผิวแพไม่มี friction -> ไม่ลากผู้เล่นซ้อนกับ follow (กันการสไลด์)
    private static PhysicsMaterial2D s_zeroFriction;

    private void Awake()
    {
        _authoredPosition = transform.position;

        // ให้ NetworkRigidbody2D ใน prefab วาดลง child เปล่าแทน root -> ตำแหน่งที่เห็นมาจาก Render ของเราที่เดียว
        // (แก้ด้วยโค้ดแทนการถอด component ออกจาก prefab ที่ฉากอ้างลำดับ behaviour ไว้)
        if (TryGetComponent<NetworkRigidbody2D>(out var netRb))
        {
            var unusedTarget = new GameObject("NetworkRigidbodyUnusedTarget").transform;
            unusedTarget.SetParent(transform, false);
            netRb.InterpolationTarget = unusedTarget;
        }
    }

    public override void Spawned()
    {
        // client จำลองแพเองด้วย -> ผู้เล่นฝั่ง client ที่ยืนบนแพไม่ถูกดึงกลับ (NJ-7)
        Runner.SetIsSimulated(Object, true);

        if (HasStateAuthority) StartPosition = _authoredPosition;
        startPosition = StartPosition;
        VerifyLog.Log($"platform '{name}' start={startPosition} transformAtSpawned={transform.position} tick={Runner.Tick.Raw}"); // TEMP verify P-1

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

    private Vector3 PositionAt(float time)
    {
        float sineValue = (Mathf.Sin(time * speed) + 1f) / 2f;
        float movementOffset = sineValue * distance;

        Vector3 position = startPosition;

        if (isVertical) position.y += movementOffset;
        else position.x += movementOffset;

        return position;
    }

    private float TickTime(float tick) => tick * Runner.DeltaTime;

    // ตำแหน่งแพ ณ เวลาใดก็ได้ — ตัวละครที่ถูกวาดย้อนหลัง (ของอีกเครื่อง) ใช้เทียบว่าแพขยับไปเท่าไรแล้ว (P-2)
    public Vector3 PositionAtTime(float time) => PositionAt(time);

    // เวลาที่แพถูกวาดในเครื่องนี้: ระหว่าง tick ก่อนหน้ากับ tick ล่าสุด — จังหวะเดียวกับตัวผู้เล่นที่ทำนายอยู่ในเครื่องนี้
    public float LocalRenderTime => TickTime(Runner.Tick.Raw - 1 + Runner.LocalAlpha);

    // ต้น tick (รวมตอน resimulate): วางแพที่ตำแหน่งปลาย tick ก่อนหน้า ก่อน FixedUpdateNetwork ของใครก็ตาม
    // ทุกเครื่องจึงเห็นจุดเริ่มเดียวกัน — ผู้เล่นที่ยืนบนอ่านตำแหน่งแพตรงนี้ไป follow
    void IBeforeTick.BeforeTick()
    {
        if (rb == null) return;

        Vector3 position = PositionAt(TickTime(Runner.Tick.Raw - 1));
        transform.position = position;
        rb.position = position;
    }

    public override void FixedUpdateNetwork()
    {
        // แค่ขยับตัวเอง — ผู้เล่นที่ยืนอยู่บนจะ follow เองผ่าน IRideablePlatform
        // (รับน้ำหนักแนวตั้งจาก kinematic collision, ส่วนแนวนอน player ตามใน MovementCharacter)
        rb.MovePosition(PositionAt(TickTime(Runner.Tick.Raw)));
    }

    public override void Render()
    {
        transform.position = PositionAt(LocalRenderTime);
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
