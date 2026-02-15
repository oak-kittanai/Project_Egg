using Fusion;
using UnityEngine;

public class Duck_Moveset : MovementCharacter
{
    [Header("Duck Settings")]
    [SerializeField] bool onWater;

    [Networked] public bool AlreadyFloating { get; set; }

    [Networked] public bool _wasJumpPressed { get; set; }

    protected override void OnFixedUpdateSpecific()
    {
        if (GetInput(out NetworkInputData input))
        {
            HandleWaterLogic(input);
        }

        if (isWaterSurface)
        {
            cAnimation.UpdateGroundTypeOnDuck(onWater);
        }
    }

    public void HandleWaterLogic(NetworkInputData input)
    {
        bool isPressed = input.jump && !_wasJumpPressed;

        _wasJumpPressed = input.jump;
    }
}