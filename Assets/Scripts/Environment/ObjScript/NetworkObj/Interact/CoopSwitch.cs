using Fusion;
using UnityEngine;
public class CoopSwitch : NetworkBehaviour, Interactable
{
    [Networked] public NetworkBool IsOn { get; set; }

    [Header("Visuals")]
    public SpriteRenderer sr;
    [SerializeField] Sprite switchOff;
    [SerializeField] Sprite switchOn;

    public void Interact(MovementCharacter player)
    {
        if (HasStateAuthority)
        {
            IsOn = !IsOn;
        }
        else
        {
            RPC_ToggleSwitch();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ToggleSwitch()
    {
        IsOn = !IsOn;
    }

    public bool CanInteract(MovementCharacter player)
    {
        return true;
    }

    public override void Render()
    {
        if (sr != null)
        {
            sr.sprite = IsOn ? switchOn : switchOff;
        }
    }
}