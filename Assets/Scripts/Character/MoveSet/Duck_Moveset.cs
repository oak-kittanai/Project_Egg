using Fusion;
using UnityEngine;

public class Duck_Moveset : MovementCharacter
{
    [Header("Duck Settings")]
    [SerializeField] bool onWater;

    protected override void OnFixedUpdateSpecific()
    {
        if (IsGrounded == false)
        {
            // Simple check ground
            isFloating = false;
        }
        else
        {
            cAnimation.UpdateGroundTypeOnDuck(onWater);
        }

        if (cAnimation != null)
        {
            cAnimation.UpdateAnimationOnDuck(new Vector2(MoveInput.x, GetComponent<Rigidbody2D>().linearVelocity.y));
        }
    }
}