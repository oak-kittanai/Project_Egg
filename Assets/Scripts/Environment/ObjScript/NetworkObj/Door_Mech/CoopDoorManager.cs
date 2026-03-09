using Fusion;
using UnityEngine;

public class CoopDoorManager : NetworkBehaviour
{
    public SlidingNetworkDoor targetDoor;

    [Header("Condition Setting")]
    public bool openOnceAndStayOpen = false;

    [Networked] public NetworkBool HasOpenedPermanently { get; set; }

    [Tooltip("StepButton")]
    public CoopStepButton[] requiredStepButtons;

    [Tooltip("Switch")]
    public CoopSwitch[] requiredInteractSwitches;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || targetDoor == null) return;

        if (openOnceAndStayOpen && HasOpenedPermanently) return;

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

        if (allConditionsMet && openOnceAndStayOpen)
        {
            HasOpenedPermanently = true;
        }

        targetDoor.SetDoorState(allConditionsMet);
    }
}