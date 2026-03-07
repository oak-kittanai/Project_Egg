using Fusion;
using UnityEngine;
public class Toggle_Presure_Trap : NetworkBehaviour, Interactable
{
    [SerializeField] TrapPressure[] targetTraps;

    [Networked] public NetworkBool isDisable { get; set; }
    public void Interact()
    {
        if (HasStateAuthority)
        {
            ToggleTrap();
        }
        else
        {
            RPC_ToggleTrap();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ToggleTrap()
    {
        ToggleTrap();
    }

    private void ToggleTrap()
    {
        isDisable = !isDisable;

        foreach (TrapPressure trap in targetTraps)
        {
            if (trap != null)
            {
                trap.SetTrapActive(!isDisable);
            }
        }
    }
}
