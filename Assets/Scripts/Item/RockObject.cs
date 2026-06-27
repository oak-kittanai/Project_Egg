using Fusion;
using UnityEngine;

public class RockObject : NetworkBehaviour, ThrowAbleItem
{
    [Header("Ref")]
    [SerializeField] NetworkObject selfNet;
    [SerializeField] Rigidbody2D rb2D;
    [SerializeField] SpriteRenderer spriteRenderer;

    [Header("Skin")]
    [Networked] int randomSkinRange { get; set; }
    [SerializeField] Sprite rock1;
    [SerializeField] Sprite rock2;
    [SerializeField] Sprite rock3;

    [Header("Setting")]
    [SerializeField] Vector3 selfPos;
    

    [SerializeField] bool isLethal = true;
    [Networked] public bool AlreadyThrow { get; set; }
    [Networked] public NetworkId ThrowerId { get; set; }

    [Header("Stun")]
    [Tooltip("rock must move faster than this to stun the duck (prevents slow rocks from stunning)")]
    [SerializeField] float stunVelocityThreshold = 3f;

    // freeze ตอน pause: เก็บค่าความเร็ว/แรงโน้มถ่วงไว้ แล้วหยุดนิ่ง คืนค่าเมื่อเล่นต่อ
    private bool _frozen;
    private Vector2 _frozenVel;
    private float _frozenAng;
    private float _frozenGravity;

    private void Awake()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || rb2D == null) return;

        // freeze ตอน pause/dialogue/tutorial — หยุดหินที่กำลังลอย/ปาอยู่ คืนค่าเมื่อเล่นต่อ
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen)
        {
            if (!_frozen)
            {
                _frozenVel = rb2D.linearVelocity;
                _frozenAng = rb2D.angularVelocity;
                _frozenGravity = rb2D.gravityScale;
                rb2D.linearVelocity = Vector2.zero;
                rb2D.angularVelocity = 0f;
                rb2D.gravityScale = 0f;
                _frozen = true;
            }
            return;
        }
        if (_frozen)
        {
            rb2D.linearVelocity = _frozenVel;
            rb2D.angularVelocity = _frozenAng;
            rb2D.gravityScale = _frozenGravity;
            _frozen = false;
        }
    }

    public override void Spawned()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();

        if (HasStateAuthority)
        {
            randomSkinRange = Random.Range(0, 3);
            UpdateSkin();
        }
    }

    private void UpdateSkin()
    {
        switch (randomSkinRange)
        {
            case 0:
                spriteRenderer.sprite = rock1;
                break;
            case 1:
                spriteRenderer.sprite = rock2;
                break;
            case 2:
                spriteRenderer.sprite = rock3;
                break;
            default:
                spriteRenderer.sprite = rock1;
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        // หินจะมีผล (kill monster / stun เป็ด) ได้เฉพาะหินที่ "ถูกปา" เท่านั้น
        // หินที่ drop จากการทุบ (ไม่มี ThrowerId) จะไม่ทำอะไรใครเลย
        bool wasThrown = ThrowerId.IsValid;

        if (isLethal && wasThrown)
        {
            // stun เป็ดได้ก็ต่อเมื่อหินยังเคลื่อนที่เร็วพอ (กันหินที่ขยับช้าๆ stun)
            bool fastEnough = rb2D.linearVelocity.magnitude >= stunVelocityThreshold;

            foreach (var hit in collision.contacts)
            {
                if (hit.collider.gameObject == gameObject) continue;

                NetworkObject hitNetObj = hit.collider.GetComponent<NetworkObject>();
                if (hitNetObj != null && hitNetObj.Id == ThrowerId)
                {
                    continue; // ข้ามคนปา ไม่ให้โดนตัวเอง
                }

                BaseMonster[] allMonsterObject = hit.collider.GetComponents<BaseMonster>();
                bool hitSomething = false;

                foreach (var monster in allMonsterObject)
                {
                    monster.InstantKill();
                    hitSomething = true;
                    break;
                }

                // stun ได้แค่เป็ดเท่านั้น และต้องเร็วพอ
                if (!hitSomething && fastEnough
                    && hit.collider.TryGetComponent<IstunAble>(out var stunnable)
                    && stunnable is Duck_Moveset)
                {
                    stunnable.TriggerStun();
                    hitSomething = true;
                }

                if (hitSomething)
                {
                    isLethal = false;

                    Vector2 pushDir = ((Vector2)rb2D.position - (Vector2)hit.collider.bounds.center).normalized;
                    if (pushDir.sqrMagnitude < 0.01f) pushDir = hit.normal;
                    rb2D.linearVelocity = Vector2.zero;
                    rb2D.AddForce(pushDir * 3f, ForceMode2D.Impulse);

                    break;
                }
            }
        }

        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Platform"))
        {
            float currentSpeed = rb2D.linearVelocity.magnitude;

            float newSpeed = Mathf.Max(0f, currentSpeed - 10f);

            if (currentSpeed > 0.1f)
            {
                rb2D.linearVelocity = rb2D.linearVelocity.normalized * newSpeed;
            }
            else
            {
                rb2D.linearVelocity = Vector2.zero;
            }
        }

        if (AlreadyThrow)
        {
            AlreadyThrow = false;
            isLethal = false;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void PickupItem_RPC(MovementCharacter player)
    {
        if (AlreadyThrow) return;

        if (player.HeldItemName.ToString() != "")
        {
            return;
        }

        if (Object != null && Object.IsValid)
        {
            player.HeldItemName = "Rock";

            GameManager.Instance.RequestDespawn(selfNet);
        }
    }

    public bool PickupItem()
    {
        return true;
    }

    public bool IsStationary()
    {
        return rb2D != null && rb2D.linearVelocity.sqrMagnitude <= 0.0001f;
    }
}
