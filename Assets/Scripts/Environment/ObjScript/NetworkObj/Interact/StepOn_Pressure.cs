using Fusion;
using UnityEngine;

public class Stepon_Pressure : NetworkBehaviour
{
    public enum PressureMode
    {
        Reusable,
        OneTimeUse
    }

    [Header("Settings")]
    [SerializeField] private TrapPressure[] targetTraps;
    [SerializeField] public LayerMask triggerLayers;
    [SerializeField] private PressureMode mode = PressureMode.Reusable;

    [Networked] public NetworkBool IsPressed { get; set; }
    [Networked] public int ObjectsOnPad { get; set; }

    [Networked] public NetworkBool IsPermanentlyActivated { get; set; }

    [Header("Visuals")]
    public SpriteRenderer sr;
    public Sprite unpressed;
    public Sprite pressed;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (IsPermanentlyActivated) return;

        if (((1 << collision.gameObject.layer) & triggerLayers) != 0)
        {
            ObjectsOnPad++;

            if (ObjectsOnPad == 1)
            {
                IsPressed = true;
                SetTrapsState(false);

                if (mode == PressureMode.OneTimeUse)
                {
                    IsPermanentlyActivated = true;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (IsPermanentlyActivated) return;

        if (((1 << collision.gameObject.layer) & triggerLayers) != 0)
        {
            ObjectsOnPad--;

            if (ObjectsOnPad <= 0)
            {
                ObjectsOnPad = 0;
                IsPressed = false;
                SetTrapsState(true);
            }
        }
    }

    private void SetTrapsState(bool activeState)
    {
        foreach (TrapPressure trap in targetTraps)
        {
            if (trap != null)
            {
                trap.SetTrapActive(activeState);
            }
        }
    }

    public override void Render()
    {
        if (sr != null)
        {
            sr.sprite = IsPressed ? pressed : unpressed;
        }
    }
}