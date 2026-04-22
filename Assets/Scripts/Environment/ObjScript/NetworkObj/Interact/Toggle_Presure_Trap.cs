using Fusion;
using UnityEngine;

public class Toggle_Presure_Trap : NetworkBehaviour, Interactable
{
    [SerializeField] TrapPressure[] targetTraps;

    [Networked] public NetworkBool isDisable { get; set; }
    [Networked] private TickTimer interactCooldownTimer { get; set; }
    [SerializeField] float interactCooldown = 0.5f;

    [Header("Visuals (Optional)")]
    public SpriteRenderer sr;
    public Sprite switchOnSprite;
    public Sprite switchOffSprite;

    public void Interact(MovementCharacter player)
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
        if (!interactCooldownTimer.ExpiredOrNotRunning(Runner)) return;

        interactCooldownTimer = TickTimer.CreateFromSeconds(Runner, interactCooldown);

        isDisable = !isDisable;

        foreach (TrapPressure trap in targetTraps)
        {
            if (trap != null)
            {
                trap.SetTrapActive(!isDisable);
            }
        }
    }

    public override void Render()
    {
        if (sr != null)
        {
            sr.sprite = isDisable ? switchOffSprite : switchOnSprite;
        }
    }
}