using Fusion;
using UnityEngine;

public class ThrowAble : NetworkBehaviour, ThrowAbleItem
{
    [Header("Setting")]
    public string itemName;
    [SerializeField] Vector3 selfPos;
    [SerializeField] NetworkObject selfNet;
    [Networked] public bool AlreadyThrow {get; set;}

    private void Awake()
    {
        selfNet = GetComponent<NetworkObject>();
    }

    public override void Spawned()
    {
        selfNet = GetComponent<NetworkObject>();
    }

    public bool PickupItem()
    {
        GameManager.Instance.RequestDespawn(selfNet);
        return true;
    }
}
