using Fusion;
using UnityEngine;

public class BreakableRock : NetworkBehaviour, Interactable
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Collider2D coll;

    [Networked, OnChangedRender(nameof(OnRockBroken))] public NetworkBool isBroken { get; set; }

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
        if (player is Duck_Moveset duck && canDrop && duck.isSmashUnlocked)
        {
            return true;
        }
        return false;
    }

    public void Interact(MovementCharacter player)
    {
        if (!HasStateAuthority) return;

        if (player is Duck_Moveset duck && duck.isSmashUnlocked)
        {
            lastBreaker = duck;
            duck.PlayHitAnimation_RPC();
            RPC_BreakRock();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_BreakRock()
    {
        if (itemToDrop != null && canDrop)
        {
            animator.SetTrigger("Trigger");

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
        DisableCollider();
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