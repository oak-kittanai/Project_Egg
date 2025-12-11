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
        currentSkin = stats.skinType;
    }

    public void Setup()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        stats = GetComponent<CharacterStats>();
        action = GetComponentInChildren<CharacterAction>();
    }

    public void UpdateSkin(characterType skin)
    {
        if (skin == currentSkin)
        {
            animator.runtimeAnimatorController = DuckController;
        }
        else
        {
            animator.runtimeAnimatorController = BirdController;
        }
    }

    public void UpdateAnimation(Vector2 direction)
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

    public void FlyAnimation()
    {
        animator.Play("Fly", 0);

        animator.SetBool("Carrying", false);
    }

    public void OnGroundCheck()
    {
        animator.SetBool("Falling", false);
    }
}
