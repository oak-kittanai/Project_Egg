using Fusion;
using UnityEngine;

public class BreakableRock : NetworkBehaviour, Interactable
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Collider2D coll;

    [Networked, OnChangedRender(nameof(OnRockBroken))] public NetworkBool isBroken { get; set; }

    // จังหวะหินแตกหลังเริ่มท่าทุบ (FB2-4) — clip แตกเริ่มทันทีที่ isBroken เปลี่ยน รอยร้าวขึ้นที่ ~0.43 วิของ clip
    // จึงตรงกับค้อนลง (~0.5 วิของท่าทุบ) — เดิม SetTrigger แล้วรอ exit time ของ state ว่างได้ถึง ~1 วิ ท่าทุบจบก่อนหินแตก
    [Tooltip("หน่วงหลังเริ่มท่าทุบก่อนหินเริ่มแตก (วินาที)")]
    [SerializeField] float breakDelay = 0.1f;
    [Networked] private TickTimer BreakTimer { get; set; }

    [SerializeField] NetworkObject selfNet;
    [SerializeField] NetworkObject itemToDrop;
    [SerializeField] int dropAmount = 1;

    [SerializeField] bool canDrop;
    [SerializeField] bool isPlayerSpecific;

    [SerializeField] Sprite alreadyBreakRock;
    [SerializeField] Animator animator;

    private Duck_Moveset lastBreaker;

    private void Awake()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (coll == null) coll = GetComponent<Collider2D>();
    }

    public override void Spawned()
    {
        if (isBroken)
        {
            DisableCollider();
            ChangeSprite();
        }
        else if (animator != null)
        {
            // เผื่อ object ถูก reuse/pool กลับมาในสภาพ animator ถูกปิดค้างจากครั้งก่อน
            animator.enabled = true;
        }
    }

    public bool CanInteract(MovementCharacter player)
    {
        if (player is Duck_Moveset duck && canDrop && !isBroken && !BreakTimer.IsRunning && duck.isSmashUnlocked)
        {
            return true;
        }
        return false;
    }

    public void Interact(MovementCharacter player)
    {
        if (!HasStateAuthority) return;

        if (player is Duck_Moveset duck && duck.isSmashUnlocked && canDrop && !isBroken && !BreakTimer.IsRunning)
        {
            lastBreaker = duck;
            if (duck.cAnimation != null) duck.cAnimation.SmashAnimation(); // host ตั้งท่าทุบ ทุกเครื่องเล่นตามจาก state ที่ sync
            BreakTimer = TickTimer.CreateFromSeconds(Runner, breakDelay);
        }
    }

    // host: ครบเวลาแล้วหินแตก — ทุกเครื่องเห็นผ่าน isBroken (เดิมใช้ RPC_BreakRock)
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !BreakTimer.Expired(Runner)) return;

        BreakTimer = TickTimer.None;

        if (itemToDrop != null && canDrop)
        {
            for (int i = 0; i < dropAmount; i++)
            {
                SpawnItem();
            }
            canDrop = false;

            isBroken = true;
        }
    }

    public void OnRockBroken()
    {
        if (!isBroken) return;

        canDrop = false;
        DisableCollider();
        PlayBreakClip();
    }

    // เล่น clip แตกทันที (ไม่ผ่าน transition ที่มี exit time) — จบ clip แล้ว Animation Event เรียก ChangeSprite
    private void PlayBreakClip()
    {
        if (animator == null || !animator.isActiveAndEnabled) return;

        string clip = animator.GetBool("IsVer1") ? "Version1" : "Version2";
        if (animator.HasState(0, Animator.StringToHash(clip))) animator.Play(clip, 0, 0f);
        else ChangeSprite();
    }

    private void DisableCollider()
    {
        if (coll != null) coll.enabled = false;
    }

    // เปลี่ยน sprite อย่างเดียว — เรียกจาก Animation Event ตอน clip จบ (และจาก Spawned สำหรับคนเข้าทีหลัง)
    public void ChangeSprite()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = alreadyBreakRock;
        }

        // หินแตกเป็นสถานะสุดท้ายแบบ static — ปิด Animator ไม่ให้ break clip วนกลับมาขับ sprite
        // เฟรมแรก (Rock-Sheet_0) ทับ Rockdrop ที่เพิ่ง set (ต้นเหตุที่ "จบ animation แล้วกลับเป็น sprite แรก")
        if (animator != null) animator.enabled = false;
    }

    public void SpawnItem()
    {
        if (!HasStateAuthority) return;
        GameManager.Instance.SpawnDropItem(itemToDrop, transform.position, lastBreaker);
    }
}