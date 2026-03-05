using Fusion;
using UnityEngine;

public class StepButton : NetworkBehaviour
{
    [SerializeField] private NetworkDoor targetDoor;
    [SerializeField] private LayerMask triggerLayers;

    [Networked] public int ObjectsOnPad { get; set; }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (((1 << collision.gameObject.layer) & triggerLayers) != 0)
        {
            ObjectsOnPad++;
            UpdateDoor();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (((1 << collision.gameObject.layer) & triggerLayers) != 0)
        {
            ObjectsOnPad--;
            UpdateDoor();
        }
    }

    private void UpdateDoor()
    {
        bool isPressed = ObjectsOnPad > 0;
        if (targetDoor != null)
        {
            targetDoor.SetDoorState(isPressed);
        }
    }
}