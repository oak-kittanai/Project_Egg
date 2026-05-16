using Fusion;
using UnityEngine;

public class ToggleButton_ReversPresure : NetworkBehaviour, Interactable
{
    public enum PressureMode
    {
        Reusable,
        OneTimeUse
    }

    [Header("Setting")]
    [SerializeField] private TrapPressure[] targetTraps;
    [SerializeField] private PressureMode mode = PressureMode.Reusable;

    [SerializeField] private bool reverseOnPress = true;

    [Networked] public NetworkBool IsPressed { get; set; }
    [Networked] public NetworkBool IsPermanentlyActivated { get; set; }

    [Header("Visuals")]
    public SpriteRenderer sr;
    public Sprite unpressed;
    public Sprite pressed;

    public void Interact(MovementCharacter player)
    {
        if (HasStateAuthority)
        {
            TogglePressure();
        }
    }

    private void TogglePressure()
    {
        if (IsPermanentlyActivated) return;

        IsPressed = !IsPressed;

        SetTrapsDirection(IsPressed ? reverseOnPress : !reverseOnPress);

        if (IsPressed && mode == PressureMode.OneTimeUse)
        {
            IsPermanentlyActivated = true;
        }
    }

    private void SetTrapsDirection(bool isReversed)
    {
        if (targetTraps == null) return;

        foreach (TrapPressure trap in targetTraps)
        {
            if (trap != null)
            {
                trap.ChangeDirection(isReversed);
            }
        }
    }

    public bool CanInteract(MovementCharacter player)
    {
        return true;
    }

    public override void Render()
    {
        if (sr != null)
        {
            sr.sprite = IsPressed ? pressed : unpressed;
        }
    }
}