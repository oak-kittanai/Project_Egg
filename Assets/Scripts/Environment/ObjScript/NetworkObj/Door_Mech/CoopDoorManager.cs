using Fusion;
using UnityEngine;

public class CoopDoorManager : NetworkBehaviour
{
    public SlidingNetworkDoor targetDoor;

    [Header("Condition Setting")]
    [Tooltip("StepButton")]
    public CoopStepButton[] requiredStepButtons;

    [Tooltip("Switch")]
    public CoopSwitch[] requiredInteractSwitches;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        bool allConditionsMet = true;

        foreach (var btn in requiredStepButtons)
        {
            if (!btn.IsPressed) allConditionsMet = false;
        }

        foreach (var sw in requiredInteractSwitches)
        {
            if (!sw.IsOn) allConditionsMet = false;
        }

        if (targetDoor != null && targetDoor.IsOpen != allConditionsMet)
        {
            targetDoor.SetDoorState(allConditionsMet);
        }
    }
}