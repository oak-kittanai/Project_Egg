using Fusion;
using UnityEngine;

public class InteractableSwitch : NetworkBehaviour, Interactable
{
    [SerializeField] private NetworkDoor targetDoor;
    [SerializeField] private bool requireKey = false;
    [Networked] public NetworkBool IsOn { get; set; }

    public void Interact()
    {
        if (requireKey)
        {
            if (GameManager.Instance.TeamKeys > 0 && !IsOn)
            {
                GameManager.Instance.UseKey();
                ToggleSwitch();
            }
            else if (!IsOn)
            {
                Debug.Log("ประตูล็อค! ต้องไปหากุญแจมาก่อน");
            }
        }
        else
        {
            ToggleSwitch();
        }
    }

    private void ToggleSwitch()
    {
        if (HasStateAuthority) ExecuteToggle();
        else RPC_ToggleSwitch();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ToggleSwitch() => ExecuteToggle();

    private void ExecuteToggle()
    {
        IsOn = !IsOn;
        if (targetDoor != null)
        {
            targetDoor.SetDoorState(IsOn);
        }
    }
}