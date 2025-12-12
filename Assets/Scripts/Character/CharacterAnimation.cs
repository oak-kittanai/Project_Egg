using Fusion;
using System.Collections;
using UnityEngine;

public class CharacterAnimation : NetworkBehaviour
{
    [Header("Referent")]
    CharacterStats stats;
    Animator animator;
    SpriteRenderer spriteRenderer;
    CharacterAction action;

    [Header("Position")]
    [Networked] public Vector3 Direction {  get; set; }
    [Networked] public int State { get; set; }
    [Networked] public bool FlipX { get; set; }

    [SerializeField] public bool Carrying => action.isCarry;

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
        UpdateSkin(stats.skinType);

        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            Debug.LogError("Animator not found after spawn");
            if (animator == null)
            {
                Debug.Log("can't get animator");
            }
        }
    }

    public void Setup()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stats = GetComponent<CharacterStats>();
        action = GetComponent<CharacterAction>();
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

    public void UpdateAnimationOnBird(Vector2 direction)
    {
        Direction = direction;

        animator.SetFloat("X", Direction.x);

        if (Direction.x < -0.01f)
        {
            FlipX = false;
        }

        if (Direction.x > 0.01f)
        {
            FlipX = true;
        }

        spriteRenderer.flipX = FlipX;
        animator.SetFloat("Y", Direction.y);
    }

    public void UpdateAnimationOnDuck(Vector2 direction, bool isWaterGround)
    {
        Direction = direction;
        if (isWaterGround)
        {
            animator.SetBool("OnWater", true);
        }
        else animator.SetBool("OnWater", false);

        animator.SetFloat("X", Direction.x);

        if (Direction.x < -0.01f)
        {
            FlipX = false;
        }

        if (Direction.x > 0.01f)
        {
            FlipX = true;
        }

        spriteRenderer.flipX = FlipX;
        animator.SetFloat("Y", Direction.y);
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
