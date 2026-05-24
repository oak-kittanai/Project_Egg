using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimation : NetworkBehaviour
{
    [Header("Referent")]
    Animator animator;
    SpriteRenderer spriteRenderer;
    MovementCharacter movement;

    [Header("State")]
    [Networked] public int State { get; set; }

    [Networked] public NetworkBool FlipX { get; set; }
    [Networked] public float AnimX { get; set; }
    [Networked] public float AnimY { get; set; }
    [Networked] public NetworkString<_32> CurrentAnimState { get; set; }

    private string _lastLocalState = "";

    [SerializeField] public bool Carrying => movement != null && movement.isCarrying;
    [SerializeField] public bool BeingCarried => movement != null && movement.IsBeingCarried;
    [SerializeField] public bool isBird => movement != null && movement.isBird;

    [Networked] public TickTimer AnimationTimer { get; set; }

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

        CacheAnimatorParameters();
    }

    public void Setup()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void InitializeMovement(MovementCharacter mc)
    {
        movement = mc;
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
                if (_lastLocalState != CurrentAnimState.Value)
                {
                    PlayAnimationSafeLocal(CurrentAnimState.Value);
                    _lastLocalState = CurrentAnimState.Value;
                }
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

    public void FaceTo(float o)
    {
        if (o < -0.01f)
        {
            FlipX = false;
        }
        else if (o > 0.01f)
        {
            FlipX = true;
        }
    }

    private void PlayAnimationNetworked(string stateName)
    {
        if (!HasState(stateName))
        {
            Debug.Log($"can't find {stateName}");
            return;
        }

        if (HasStateAuthority || HasInputAuthority)
        {
            CurrentAnimState = stateName;
        }
    }

    public void SetActionAnimation(string stateName, float lockDuration)
    {
        PlayAnimationNetworked(stateName);
        if (Runner != null) AnimationTimer = TickTimer.CreateFromSeconds(Runner, lockDuration);
    }

    public void ClearAnimationLock()
    {
        if (Runner != null) AnimationTimer = TickTimer.None;
    }

    private void PlayAnimationSafeLocal(string stateName)
    {
        if (!HasState(stateName)) return;

        if (stateName == "BlendAnimation" || stateName == "NormalMovementTree" || stateName == "CarryMovementTree")
        {
            animator.Play(stateName, 0, 0f);
        }
        else
        {
            animator.Play(stateName, 0);
        }
    }

    public void ReturnToBlendAnimation()
    {
        if (Runner != null && !AnimationTimer.ExpiredOrNotRunning(Runner)) return;

        if (currentSkin == characterType.Duck && Carrying)
        {
            PlayAnimationNetworked("CarryMovementTree");
        }
        else
        {
            PlayAnimationNetworked("NormalMovementTree");
        }
    }

    public void UpdateGroundTypeOnDuck(bool isWaterGround)
    {
        if (Runner != null && !AnimationTimer.ExpiredOrNotRunning(Runner)) return;

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

    // Overall
    public void JumpAnimation() => PlayAnimationNetworked("Jump");
    public void InteractAnimation() => SetActionAnimation("Interact", 0.5f);
    public void DeathAnimation() => SetActionAnimation("Death", 999f);
    public void PrepareToRespawnAnimation() => SetActionAnimation("PrepareToRespawn", 999f);
    public void UpdateClimbAnimation(float x) => PlayAnimationNetworked("Climb");

    public void BirdPrepareFallingAnimaion()
    {

    }

    public void FallingAndFloatAnimation(bool isFalling, bool isNearGround = false)
    {
        if (movement.IsGrounded) return;

        if (!isBird)
        {
            if (Carrying)
            {
                PlayAnimationNetworked(isFalling ? "Falling_carry" : "Floating_carry");
            }
            else
            {
                if (isFalling) PlayAnimationNetworked("Falling");
            }
        }
        else
        {
            if (isFalling)
            {
                PlayAnimationNetworked("Falling");
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




    public void SmashAnimation() => SetActionAnimation("Smash", 1.27f);
    public void SwimAnimation() => PlayAnimationNetworked("Swim");
    //public void DiveAnimation() => PlayAnimationNetworked("Diving");
    public void ReturnToSurface() => PlayAnimationNetworked("Swim"); // need Animation

    // Bird
    public void ThrowAnimation() => SetActionAnimation("Throwing", 1.43f);

    public void FlyUpAnimation() => PlayAnimationNetworked("Fly_Up");
    public void FlyFloatAnimation() => PlayAnimationNetworked("Fly");
    //public void ReachGroundAnimation() => PlayAnimationNetworked("Reach_ground");
}