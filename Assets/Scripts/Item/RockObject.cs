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

    private void Awake()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
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

        if (isLethal)
        {
            foreach (var hit in collision.contacts)
            {
                if (hit.collider.gameObject == gameObject) continue;

                BaseMonster[] allMonsterObject = hit.collider.GetComponents<BaseMonster>();

                foreach (var monster in allMonsterObject)
                {
                    monster.InstantKill();
                    isLethal = false;
                    rb2D.linearVelocity = Vector2.zero;
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
            Debug.Log("hand full can't pick");
            return;
        }

        if (Object != null && Object.IsValid)
        {
            player.HeldItemName = "Rock";

            GameManager.Instance.RequestDespawn(selfNet);
            Debug.Log($"{player.name} pick Rock");
        }
    }

    public bool PickupItem()
    {
        return true;
    }
}
