using Fusion;
using UnityEngine;

public class NetworkDoor : NetworkBehaviour
{
    [Networked] public NetworkBool IsOpen { get; set; }

    [SerializeField] private Animator doorAnimator;
    [SerializeField] private Collider2D doorCollider;

    public void SetDoorState(bool open)
    {
        if (HasStateAuthority)
        {
            IsOpen = open;
        }
        else
        {
            RPC_SetDoorState(open);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetDoorState(NetworkBool open)
    {
        IsOpen = open;
    }

    public override void Render()
    {
        if (doorAnimator != null)
        {
            doorAnimator.SetBool("IsOpen", IsOpen);
        }

        if (doorCollider != null)
        {
            doorCollider.enabled = !IsOpen;
        }
    }
}