using Fusion;
using UnityEngine;

public class ThrowAble : NetworkBehaviour, ThrowAbleItem
{
    [Header("Setting")]
    public string itemName;
    [SerializeField] Vector3 selfPos;
    [SerializeField] NetworkObject selfNet;
    [SerializeField] Rigidbody2D rb2D;

    [SerializeField] bool isLethal; // เปลี่ยนชื่อจาก canBeStun เป็น isLethal เพื่อให้ตรงความหมาย
    [Networked] public bool AlreadyThrow { get; set; }

    private void Awake()
    {
        selfNet = GetComponent<NetworkObject>();
        rb2D = GetComponent<Rigidbody2D>();
    }

    public override void Spawned()
    {
        selfNet = GetComponent<NetworkObject>();
        OnCheckItemCollider();
    }

    public void OnCheckItemCollider()
    {
        if (itemName == "Rock")
        {
            isLethal = true; // ถ้าเป็นหิน จะกลายเป็นอาวุธสังหารทันที
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        if (isLethal)
        {
            foreach (var hit in collision.contacts)
            {
                if (hit.collider.gameObject == gameObject) continue;

                // เช็คว่าของที่ชนคือมอนสเตอร์ที่สืบทอดจาก BaseMonster หรือไม่
                BaseMonster[] allMonsterObject = hit.collider.GetComponents<BaseMonster>();

                foreach (var monster in allMonsterObject)
                {
                    // ไม่ต้องเช็ค Stun แล้ว สั่งตายได้เลย!
                    monster.InstantKill();
                    isLethal = false; // ป้องกันบั๊กทำดาเมจซ้ำซ้อน
                    rb2D.linearVelocity = Vector2.zero; // หินหยุดกระเด็น

                    // (ตัวเลือกเสริม): อยากให้หินแตกหายไปพร้อมมอนสเตอร์เลยไหม? 
                    // ถ้าอยากให้แตกเลย ให้เอาคอมเมนต์บรรทัดล่างออกครับ
                    // GameManager.Instance.RequestDespawn(selfNet); 

                    break;
                }
            }
        }
    }

    public bool PickupItem()
    {
        GameManager.Instance.RequestDespawn(selfNet);
        return true;
    }
}