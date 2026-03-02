using Fusion;
using System.Collections.Generic;
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

    private HashSet<int> parameterHashes = new HashSet<int>();

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

        CacheAnimatorParameters();
    }

    public void Setup()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stats = GetComponent<CharacterStats>();
    }

    public void UpdateSkin(characterType skin)
    {
        currentSkin = skin;
        if (skin == characterType.Duck)
        {
            animator.runtimeAnimatorController = DuckController;
        }
        else
        {
            animator.runtimeAnimatorController = BirdController;
        }

        CacheAnimatorParameters();
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        parameterHashes.Clear();
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            parameterHashes.Add(param.nameHash);
        }
    }

    private bool HasParameter(string paramName)
    {
        return parameterHashes.Contains(Animator.StringToHash(paramName));
    }

    private bool HasState(string stateName)
    {
        return animator.HasState(0, Animator.StringToHash(stateName));
    }

    public void UpdateAnimationController(Vector2 direction)
    {
        if (HasParameter("X")) animator.SetFloat("X", direction.x);

        if (direction.x < -0.01f) FlipX = false;
        if (direction.x > 0.01f) FlipX = true;

        spriteRenderer.flipX = FlipX;
        if (HasParameter("Y")) animator.SetFloat("Y", direction.y);
    }

    public void ReturnToBlendAnimation()
    {
        if (!HasState("BlendAnimation")) return;
        animator.CrossFade("BlendAnimation", 0.1f);
    }

    public void UpdateGroundTypeOnDuck(bool isWaterGround)
    {
        if (!HasParameter("OnWater")) return;

        if (isWaterGround)
        {
            animator.SetBool("OnWater", true);
            if (Carrying && HasState("Floating_carry"))
            {
                animator.Play("Floating_carry", 0);
            }
            else if (HasState("Floating"))
            {
                animator.Play("Floating", 0);
            }
        }
        else
        {
            animator.SetBool("OnWater", false);
        }
    }

    public void UpdateOnGroundTypeOnBird(bool isInTheAir)
    {
        if (!HasParameter("InTheAir")) return;
        animator.SetBool("InTheAir", isInTheAir);
    }

    public void UpdateFloatingOnBird(bool Floating)
    {
        if (!HasParameter("InTheAir")) return;
        animator.SetBool("InTheAir", Floating);
    }

    // Overall

    public void JumpAnimation()
    {
        if (!HasState("Jump")) return;
        animator.Play("Jump", 0);
    }

    public void FallingAndFloatAnimation(bool isFalling)
    {
        if (Carrying)
        {
            if (HasParameter("Carrying")) animator.SetBool("Carrying", !isFalling);
            if (HasParameter("Falling")) animator.SetBool("Falling", isFalling);

            if (!isFalling && HasState("Floating_carry"))
            {
                animator.Play("Floating_carry", 0);
            }
        }
        else
        {
            if (HasParameter("Falling")) animator.SetBool("Falling", isFalling);
            if (HasParameter("Float")) animator.SetBool("Float", !isFalling);
        }
    }

    // Duck

    public void UpdateSwimFlip(Vector2 direction)
    {
        if (HasParameter("X")) animator.SetFloat("X", direction.x);

        if (direction.x < -0.01f) FlipX = true;
        if (direction.x > 0.01f) FlipX = false;

        spriteRenderer.flipX = FlipX;

        if (HasParameter("Y")) animator.SetFloat("Y", direction.y);
    }

    public void SetFlipToFalse()
    {
        FlipX = false;
    }

    public void SmashAnimation()
    {
        if (!HasState("Hit")) return;
        animator.Play("Hit", 0);
    }

    public void SwimAnimation()
    {
        if (!HasState("Swim")) return;
        animator.Play("Swim", 0);
    }

    public void DiveAnimation()
    {
        if (!HasParameter("Diving")) return;
        animator.SetBool("Diving", true);
    }

    public void ReturnToSurface()
    {
        if (!HasState("Swim")) return;
        animator.Play("Swim", 0);
    }

    // Bird

    public void ThrowAnimation()
    {
        if (!HasParameter("Throwing")) return;
        animator.SetBool("Throwing", true);
    }

    public void FlyAnimation(bool isFly)
    {
        if (HasParameter("Flying")) animator.SetBool("Flying", isFly);

        if (isFly && HasState("Fly"))
        {
            animator.Play("Fly", 0);
        }
    }

    public void OnGroundCheck()
    {
        if (!HasParameter("Falling")) return;
        animator.SetBool("Falling", false);
    }
}