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
    }

    public bool PickupItem()
    {
        GameManager.Instance.RequestDespawn(selfNet);
        return true;
    }
}