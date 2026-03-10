using Fusion;
using UnityEngine;

public class CoopDoorManager2 : NetworkBehaviour
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

        bool anyConditionMet = false;

        if (requiredStepButtons != null)
        {
            foreach (var btn in requiredStepButtons)
            {
                if (btn == null) continue;

                if (btn.IsPressed)
                {
                    anyConditionMet = true;
                    break;
                }
            }
        }

        if (!anyConditionMet && requiredInteractSwitches != null)
        {
            foreach (var sw in requiredInteractSwitches)
            {
                if (sw != null && sw.IsOn)
                {
                    anyConditionMet = true;
                    break;
                }
            }
        }

        if (anyConditionMet && openOnceAndStayOpen)
        {
            HasOpenedPermanently = true;
        }

        targetDoor.SetDoorState(anyConditionMet);
    }
}