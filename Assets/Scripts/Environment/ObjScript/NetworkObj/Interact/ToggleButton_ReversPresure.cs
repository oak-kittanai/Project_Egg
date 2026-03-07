using Fusion;
using UnityEngine;
public class ToggleButton_ReversPresure : NetworkBehaviour, Interactable
{
    [SerializeField] TrapPressure[] targetTrap;
    [Networked] public NetworkBool isRevers { get; set; }
    public void Interact()
    {
        if (HasStateAuthority)
        {
            ToggleReverse();
        }
        else
        {
            RPC_ToggleReverse();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ToggleReverse()
    {
        ToggleReverse();
    }

    private void ToggleReverse()
    {
        isRevers = !isRevers;

        foreach (var trap in targetTrap)
        {
            if (trap != null)
            {
                trap.ChangeDirection(isRevers);
            }
        }
    }
}