using Fusion;
using UnityEngine;

public class ToggleButton_ReversPresure : NetworkBehaviour, Interactable
{
    [SerializeField] TrapPressure[] targetTrap;

    [Networked] public NetworkBool isRevers { get; set; }
    [Networked] private TickTimer interactCooldownTimer { get; set; }
    [SerializeField] float interactCooldown = 0.5f;

    [Header("Visuals (Optional)")]
    public SpriteRenderer sr;
    public Sprite normalSprite;
    public Sprite reversedSprite;

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
        if (!interactCooldownTimer.ExpiredOrNotRunning(Runner)) return;

        interactCooldownTimer = TickTimer.CreateFromSeconds(Runner, interactCooldown);

        isRevers = !isRevers;

        foreach (var trap in targetTrap)
        {
            if (trap != null)
            {
                trap.ChangeDirection(isRevers);
            }
        }
    }

    public override void Render()
    {
        if (sr != null)
        {
            sr.sprite = isRevers ? reversedSprite : normalSprite;
        }
    }
}