using Fusion;
using UnityEngine;

public class B_Interact_Door : NetworkBehaviour, Interactable
{
    [Header("Target Door")]
    [SerializeField] Interact_Door targetDoor;

    [Header("Visual Feedback")]
    [SerializeField] Sprite unpressed;
    [SerializeField] Sprite pressed;

    private SpriteRenderer spriteRenderer;

    [Networked, OnChangedRender(nameof(OnSwitchToggled))]
    public NetworkBool isToggled { get; set; }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (unpressed != null) spriteRenderer.sprite = unpressed;
    }

    public override void Spawned()
    {
        UpdateStatus();
    }

    public void Interact()
    {
        if (HasStateAuthority)
        {
            isToggled = !isToggled;
        }
        else
        {
            RPC_ToggleSwitch();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ToggleSwitch()
    {
        isToggled = !isToggled;
    }

    public void OnSwitchToggled()
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = isToggled ? pressed : unpressed;
        }

        if (targetDoor != null)
        {
            targetDoor.SetDoorState(isToggled);
        }
    }
}