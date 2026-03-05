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

    [Networked] public NetworkBool FlipX { get; set; }
    [Networked] public float AnimX { get; set; }
    [Networked] public float AnimY { get; set; }
    [Networked] public NetworkString<_32> CurrentAnimState { get; set; }

    [SerializeField] public bool Carrying => movement is Duck_Moveset duck && duck.IsCarrying;
    [SerializeField] public bool BeingCarried => movement.IsBeingCarried;

    [Header("Controller Setting")]
    [SerializeField] public RuntimeAnimatorController DuckController;
    [SerializeField] public RuntimeAnimatorController BirdController;

    [SerializeField] public characterType currentSkin;

    private HashSet<int> parameterHashes = new HashSet<int>();

    private void Awake()
    {
        Setup();
    }

    public override void Spawned()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (movement == null) movement = GetComponent<MovementCharacter>();

        CacheAnimatorParameters();
    }

    public void Setup()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stats = GetComponent<CharacterStats>();
    }

    public override void Render()
    {
        if (spriteRenderer != null) spriteRenderer.flipX = FlipX;

        if (animator != null)
        {
            if (HasParameter("X")) animator.SetFloat("X", AnimX);
            if (HasParameter("Y")) animator.SetFloat("Y", AnimY);

            if (CurrentAnimState.Value != string.Empty)
            {
                PlayAnimationSafeLocal(CurrentAnimState.Value);
            }
        }
    }

    public void UpdateSkin(characterType skin)
    {
        currentSkin = skin;
        animator.runtimeAnimatorController = (skin == characterType.Duck) ? DuckController : BirdController;
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

    private bool HasParameter(string paramName) => parameterHashes.Contains(Animator.StringToHash(paramName));
    private bool HasState(string stateName) => animator.HasState(0, Animator.StringToHash(stateName));

    public void UpdateAnimationController(Vector2 direction)
    {
        if (HasStateAuthority || HasInputAuthority)
        {
            AnimX = direction.x;
            AnimY = direction.y;

            if (direction.x < -0.01f) FlipX = false;
            if (direction.x > 0.01f) FlipX = true;
        }
    }

    private void PlayAnimationNetworked(string stateName)
    {
        if (!HasState(stateName)) return;

        if (HasStateAuthority || HasInputAuthority)
        {
            CurrentAnimState = stateName;
        }
    }

    private void PlayAnimationSafeLocal(string stateName)
    {
        if (!HasState(stateName)) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(stateName))
        {
            if (stateName == "BlendAnimation" || stateName == "NormalMovementTree" || stateName == "CarryMovementTree")
            {
                animator.CrossFade(stateName, 0.1f);
            }
            else
            {
                animator.Play(stateName, 0);
            }
        }
    }

    public void ReturnToBlendAnimation()
    {
        if (currentSkin == characterType.Duck && Carrying)
        {
            PlayAnimationNetworked("CarryMovementTree");
        }
        else
        {
            PlayAnimationNetworked("NomalMovementTree");
        }
    }

    public void UpdateGroundTypeOnDuck(bool isWaterGround)
    {
        if (isWaterGround)
        {
            bool isMoving = Mathf.Abs(AnimX) > 0.01f;

            if (Carrying)
            {
                PlayAnimationNetworked(isMoving ? "Floating_carry_walk" : "Floating_carry_Idle");
            }
            else
            {
                PlayAnimationNetworked(isMoving ? "Floating_walk" : "Floating_Idle");
            }
        }
    }

    public void UpdateOnGroundTypeOnBird(bool isInTheAir)
    {
        if (isInTheAir) PlayAnimationNetworked("InTheAir");
        else ReturnToBlendAnimation();
    }

    public void UpdateFloatingOnBird(bool isFloating)
    {
        if (isFloating)
        {
            if (HasState("Float")) PlayAnimationNetworked("Float");
            else PlayAnimationNetworked("Floating");
        }
        else PlayAnimationNetworked("Falling");
    }

    // Overall
    public void JumpAnimation() => PlayAnimationNetworked("Jump");

    public void InteractAnimation() => PlayAnimationNetworked("Interact");

    public void FallingAndFloatAnimation(bool isFalling, bool isNearGround = false)
    {
        if (Carrying)
        {
            if (isFalling) PlayAnimationNetworked("Falling_carry");
            else PlayAnimationNetworked("Floating_carry");
        }
        else
        {
            if (isFalling)
            {
                /* if (isNearGround && HasState("Reach_ground")) 
                {
                    PlayAnimationNetworked("Reach_ground");
                }
                else 
                */
                if (HasState("Float_Down"))
                {
                    PlayAnimationNetworked("Float_Down");
                }
                else
                {
                    PlayAnimationNetworked("Falling");
                }
            }
            else
            {
                PlayAnimationNetworked("Float");
            }
        }
    }

    // Duck
    public void UpdateSwimFlip(Vector2 direction)
    {
        if (HasStateAuthority || HasInputAuthority)
        {
            AnimX = direction.x;
            AnimY = direction.y;

            if (direction.x < -0.01f) FlipX = true;
            if (direction.x > 0.01f) FlipX = false;
        }
    }

    public void SetFlipToFalse()
    {
        if (HasStateAuthority || HasInputAuthority) FlipX = false;
    }

    public void SmashAnimation() => PlayAnimationNetworked("Hit");
    public void SwimAnimation() => PlayAnimationNetworked("Swim");
    public void DiveAnimation() => PlayAnimationNetworked("Diving");
    public void ReturnToSurface() => PlayAnimationNetworked("Swim");

    // Bird
    public void ThrowAnimation() => PlayAnimationNetworked("Throwing");

    public void FlyAnimation(bool isFly)
    {
        if (isFly)
        {
            if (BeingCarried && HasState("Fly_carry")) PlayAnimationNetworked("Fly_carry");
            else PlayAnimationNetworked("Fly");
        }
    }
}