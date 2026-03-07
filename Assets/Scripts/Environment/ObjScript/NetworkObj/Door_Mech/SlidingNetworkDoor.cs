using Fusion;
using UnityEngine;

public class SlidingNetworkDoor : NetworkBehaviour
{
    [Networked] public NetworkBool IsOpen { get; set; }

    [Header("Sliding Settings")]
    [Tooltip("X = Right, Y = Down")]
    public Vector3 slideOffset = new Vector3(0f, 3f, 0f);
    public float slideSpeed = 3f;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    public override void Spawned()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + slideOffset;
    }

    public void SetDoorState(bool open)
    {
        if (HasStateAuthority) IsOpen = open;
        else RPC_SetDoorState(open);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetDoorState(NetworkBool open)
    {
        IsOpen = open;
    }

    public override void Render()
    {
        Vector3 targetPos = IsOpen ? openPosition : closedPosition;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, slideSpeed * Time.deltaTime);
    }
}