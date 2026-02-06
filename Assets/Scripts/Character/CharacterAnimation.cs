using Fusion;
using System.Collections;
using UnityEngine;

public class CharacterAnimation : NetworkBehaviour
{
    [Header("Referent")]
    CharacterStats stats;
    Animator animator;
    SpriteRenderer spriteRenderer;
    MovementCharacter movement;

    [Header("Position")]
    [Networked] public int State { get; set; }
    [Networked] public bool FlipX { get; set; }

    [SerializeField] public bool Carrying => movement.IsCarrying;

    [Header("Controller Setting")]
    [SerializeField] public RuntimeAnimatorController DuckController;
    [SerializeField] public RuntimeAnimatorController BirdController;

    [SerializeField] characterType currentSkin;

    private void Awake()
    {
        Setup();
    }

    public override void Spawned()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            Debug.Log("Try to get Animator");
            if (animator == null)
            {
                Debug.LogError("can't Get Animator");
            }
        }

        if (movement == null)
        {
            movement = GetComponent<MovementCharacter>();
            Debug.Log("Try to get MovementCharacter");
            if (movement == null)
            {
                Debug.LogError("can't Get MovementCharacter");
            }
        }
    }

    public void Setup()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stats = GetComponent<CharacterStats>();
    }

    public void UpdateSkin(characterType skin)
    {
        if (skin == characterType.Duck)
        {
            animator.runtimeAnimatorController = DuckController;
        }
        else
        {
            animator.runtimeAnimatorController = BirdController;
        }
    }

    public void UpdateAnimationController(Vector2 direction)
    {
        animator.SetFloat("X", direction.x);

        if (direction.x < -0.01f)
        {
            FlipX = false;
        }

        if (direction.x > 0.01f)
        {
            FlipX = true;
        }

        spriteRenderer.flipX = FlipX;
        animator.SetFloat("Y", direction.y);
    }

    public void ReturnToBlendAnimation()
    {
        animator.CrossFade("BlendAnimation", 0.1f);
    }

    public void UpdateGroundTypeOnDuck(bool isWaterGround)
    {
        if (isWaterGround)
        {
            animator.SetBool("OnWater", true);
        }
        else animator.SetBool("OnWater", false);
    }

    public void UpdateOnGroundTypeOnBird(bool isInTheAir)
    {
        if (isInTheAir)
        {
            animator.SetBool("InTheAir", true);
        }
        else animator.SetBool("InTheAir", false);
    }

    public void UpdateFloatingOnBird(bool Floating)
    {
        if (Floating)
        {
            animator.SetBool("InTheAir", true);
        }
        else animator.SetBool("InTheAir", false);
    }

    // Overall

    public void JumpAnimation()
    {
        animator.Play("Jump", 0);
    }

    public void FallingAndFloatAnimation(bool isFalling)
    {
        if (Carrying)
        {
            if (isFalling)
            {
                animator.SetBool("Carrying", false);
                animator.SetBool("Falling", true);
            }
            else
            {
                animator.SetBool("Carrying", true);
                animator.Play("Floating_carry", 0);
            }
        }
        else
        {
            if (isFalling)
            {
                animator.SetBool("Falling", true);
                animator.SetBool("Float", false);
            }
            else
            {
                animator.SetBool("Float", true);
                animator.SetBool("Falling", false);
            }
        }
    }

    // Duck

    public void SmashAnimation()
    {
        animator.Play("Hit", 0);
    }

    public void SwimAnimation()
    {
        animator.Play("Swim", 0);
    }

    public void DiveAnimation()
    {
        animator.SetBool("InWater", true);
    }

    // Bird

    public void ThrowAnimation()
    {
        animator.SetBool("Throwing", true);
    }

    public void FlyAnimation(bool isFly)
    {
        if (isFly)
        {
            animator.Play("Fly", 0);
            animator.SetBool("Flying", true);
        }
        else
        {
            animator.SetBool("Flying", false);
        }
    }

    public void OnGroundCheck()
    {
        animator.SetBool("Falling", false);
    }
}
