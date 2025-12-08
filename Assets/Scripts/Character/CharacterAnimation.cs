using Fusion;
using System.Collections;
using UnityEngine;

public class CharacterAnimation : NetworkBehaviour
{
    [Header("Referent")]
    CharacterStats stats;
    Animator animator;
    SpriteRenderer spriteRenderer;

    [Header("Position")]
    [Networked] public Vector3 Direction {  get; set; }
    [Networked] public int State { get; set; }
    [Networked] public bool FlipX { get; set; }

    [Header("Controller Setting")]
    [SerializeField] public RuntimeAnimatorController DuckController;
    [SerializeField] public RuntimeAnimatorController BirdController;

    [SerializeField] SkinType currentSkin;

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
    }

    public void UpdateSkin(SkinType skin)
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

    public void UpdateActionAnimation(int i)
    {
        State = i;

        if (currentSkin == SkinType.Bird)
        {
            if (State == 1)
            {
                animator.Play("Pre_Jump", 0);
            }
            else if (State == 2)
            {
                animator.Play("Fly", 0);
            }
            else if (State == 3)
            {
                animator.SetBool("Falling", true);
            }
            else if (State == 4)
            {
                animator.SetBool("Float", true);
            }
            else if (State == 5)
            {
                // Throwing
            }
            else
            {
                animator.SetBool("Falling", false);
                animator.SetBool("Float", false);
                animator.SetBool("Carrying", false);
            }
        }
        else
        {
            if (State == 1)
            {
                animator.Play("Jump", 0);
            }
            else if (State == 2)
            {
                animator.Play("Floating", 0);
                animator.SetBool("Float", true);
                animator.SetBool("Carrying", false);
            }
            else if (State == 3)
            {
                animator.Play("Floating_carry", 0);
                animator.SetBool("Float", true);
                animator.SetBool("Carrying", true);
            }
            else if (State == 4)
            {
                animator.Play("Swim", 0);
                animator.SetBool("InWater", true);
                animator.SetBool("Float", false);
                animator.SetBool("Carrying", false);
            }
            else
            {
                animator.SetBool("Falling", false);
                animator.SetBool("Float", false);
                animator.SetBool("InWater", false);
                animator.SetBool("Carrying", false);
            }
        }


        /*if (Action == 3)
        {
            animator.Play("Float_Down", 0);
            animator.SetBool("Falling", true);
        }*/
    }

    public void OnGroundCheck()
    {
        animator.SetBool("Falling", false);
    }
}
