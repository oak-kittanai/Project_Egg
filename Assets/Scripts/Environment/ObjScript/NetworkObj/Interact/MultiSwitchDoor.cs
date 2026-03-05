using Fusion;
using UnityEngine;

public class MultiSwitchDoor : NetworkBehaviour
{
    [SerializeField] private NetworkDoor targetDoor;

    [SerializeField] private InteractableSwitch[] requiredSwitches;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        bool allOn = true;
        foreach (var sw in requiredSwitches)
        {
            if (!sw.IsOn)
            {
                allOn = false;
                break;
            }
        }

        if (targetDoor != null)
        {
            targetDoor.SetDoorState(allOn);
        }
    }
}