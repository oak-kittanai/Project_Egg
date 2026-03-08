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
        if (!HasStateAuthority || targetDoor == null) return;

        bool allConditionsMet = true;

        if (requiredStepButtons != null)
        {
            foreach (var btn in requiredStepButtons)
            {
                if (btn == null) continue;
                if (!btn.IsPressed) allConditionsMet = false;
            }
        }

        if (requiredInteractSwitches != null)
        {
            foreach (var sw in requiredInteractSwitches)
            {
                if (sw == null) continue;
                if (!sw.IsOn) allConditionsMet = false;
            }
        }

        targetDoor.SetDoorState(allConditionsMet);
    }
}