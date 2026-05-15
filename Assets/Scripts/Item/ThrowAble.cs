using Fusion;
using UnityEngine;

public class ThrowAble : NetworkBehaviour, ThrowAbleItem
{
    [Header("Setting")]
    public string itemName;
    [SerializeField] Vector3 selfPos;
    [SerializeField] NetworkObject selfNet;
    [SerializeField] Rigidbody2D rb2D;

    [SerializeField] bool isLethal;
    [Networked] public bool AlreadyThrow { get; set; }

    private void Awake()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
    }

    public override void Spawned()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
        OnCheckItemCollider();
    }

    public void OnCheckItemCollider()
    {
        if (itemName == "Rock")
        {
            isLethal = true;
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

            float newSpeed = Mathf.Max(0f, currentSpeed - 5f);

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

        if (Object != null && Object.IsValid)
        {
            if (player != null)
            {
                player._canThrowItem = true;
            }

            GameManager.Instance.RequestDespawn(selfNet);
            Debug.Log("Try to Despawn");
        }
    }

    public bool PickupItem()
    {
        return true;
    }
}