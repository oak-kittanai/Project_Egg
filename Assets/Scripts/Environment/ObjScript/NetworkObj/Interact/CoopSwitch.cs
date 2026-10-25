using Fusion;
using UnityEngine;
public class CoopSwitch : NetworkBehaviour, Interactable
{
    [Networked] public NetworkBool IsOn { get; set; }

    [Header("Visuals")]
    public SpriteRenderer sr;
    [SerializeField] Sprite switchOff;
    [SerializeField] Sprite switchOn;

    private bool _shownOn;

    public override void Spawned()
    {
        _shownOn = IsOn;
    }

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
        // เสียงติกทุกครั้งที่สลับ (SW-2): ทุกเครื่องเล่นเองครั้งเดียวจากสถานะที่ sync มา
        if (IsOn != _shownOn)
        {
            _shownOn = IsOn;
            AudioManager.Instance?.PlaySwitchTick(transform.position);
        }

        if (sr != null)
        {
            sr.sprite = IsOn ? switchOn : switchOff;
        }
    }
}