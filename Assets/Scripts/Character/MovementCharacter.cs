using Fusion;
using Fusion.Addons.Physics;
using UnityEditor;
using UnityEngine;

public class MovementCharacter : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] public CharacterStats stats;
    [SerializeField] public CharacterAnimation cAnimation;
    [SerializeField] public Rigidbody2D rb2D;
    [SerializeField] public Collider2D coll2D;

    [Networked] public bool isBird {  get; set; }

    [Header("Movement Settings")]
    [Networked] public bool IsGrounded { get; set; }
    [Networked] public bool isWaterSurface { get; set; }
    [Networked] public bool IsInAir { get; set; }
    [Networked] public Vector2 MoveInput { get; set; }
    [Networked] public bool isFloating { get; set; }

    [SerializeField] public bool resetAnimation;

    // Falling
    [Networked] public bool IsFalling { get; set; }
    [Networked] public bool FallingBusy { get; set; }
    [SerializeField] public bool isOptional;
    [SerializeField] public float optionalGravity;
    [SerializeField] public float normalGravity = 3.5f;
    [SerializeField] public float heavyGravity = 6.5f;
    [SerializeField] public float maxGravity = 19f;

    [Header("Carry System (The New Part)")]
    [Networked] public NetworkId CarriedFriendId { get; set; }
    [Networked] public bool IsCarrying { get; set; }
    [Networked] public bool IsBeingCarried { get; set; }

    [Networked] public bool IsInteractBusy { get; set; }


    // Local Variables
    public float rayDistance = 1.2f;
    public float interactRadius = 1.5f;
    public float playerInteractRadius = 1f;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<CharacterStats>();
        if (cAnimation == null) cAnimation = GetComponent<CharacterAnimation>();
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
    }

    public override void Spawned()
    {
        if (stats.skinType == characterType.Bird)
        {
            isBird = true;
        }
        else
        {
            isBird = false;
        }

        resetAnimation = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (IsBeingCarried)
        {
            rb2D.simulated = false; // Stop gravity/collisions
            return;
        }
        else
        {
            rb2D.simulated = true;
        }

        if (GetInput(out NetworkInputData input))
        {
            HandleMovement(input);
            HandleJump(input);
            HandleInteraction(input);
        }

        CheckGround();

        if (IsCarrying)
        {
            UpdateCarriedFriendPosition();
        }

        OnFixedUpdateSpecific();
    }

    // Movement
    private void HandleMovement(NetworkInputData input)
    {
        float targetSpeed = input.horizontal * stats.maxSpeed;
        float currentSpeed = rb2D.linearVelocity.x;

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? stats.acceleration : stats.deceleration;

        float speedDif = targetSpeed - currentSpeed;
        rb2D.AddForce(Vector2.right * (speedDif * accelRate));

        cAnimation.UpdateAnimationController(new Vector2(input.horizontal, rb2D.linearVelocity.y));
    }

    protected virtual void HandleJump(NetworkInputData input)
    {
        if (input.jump && IsGrounded)
        {
            IsInteractBusy = true;
            cAnimation.JumpAnimation();
            resetAnimation = true;
            rb2D.AddForce(Vector2.up * stats.s_jumpForce, ForceMode2D.Impulse);
            IsGrounded = false;
            IsInteractBusy = false;
        }
    }

    private void HandleInteraction(NetworkInputData input)
    {
        if (input.Keyboard_E)
        {
            if (IsCarrying)
            {
                DropFriend();
                return;
            }

            Collider2D[] hitsItem = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            foreach (var hit in hitsItem)
            {
                if (hit.gameObject == gameObject) continue;

                if (hit.TryGetComponent<Interactable>(out var interactable))
                {
                    Debug.Log("Trigger interactable object");
                    interactable.Interact();
                    return;
                }
            }

            Collider2D[] hitsPlayer = Physics2D.OverlapCircleAll(transform.position, playerInteractRadius);
            foreach (var hit in hitsPlayer)
            {
                if (hit.gameObject == gameObject) continue;

                if (hit.TryGetComponent<MovementCharacter>(out var otherPlayer))
                {
                    PickupFriend(otherPlayer);
                    return;
                }
            }
        }
    }

    public void PickupFriend(MovementCharacter friend)
    {
        IsInteractBusy = true;
        Debug.Log("Picking up friend!");
        IsCarrying = true;
        CarriedFriendId = friend.Object.Id;

        friend.SetCarriedState(true);
    }

    public void DropFriend()
    {
        if (Runner.TryFindObject(CarriedFriendId, out var obj))
        {
            if (obj.TryGetComponent<MovementCharacter>(out var friend))
            {
                friend.SetCarriedState(false);
                // Optional: Throw them slightly forward
                friend.GetComponent<Rigidbody2D>().AddForce(new Vector2(transform.localScale.x * 2, 2), ForceMode2D.Force);
            }
        }

        IsCarrying = false;
        IsInteractBusy = false;
        resetAnimation = true;
        CarriedFriendId = default;
    }

    // set state on clients
    public void SetCarriedState(bool state)
    {
        IsBeingCarried = state;
        // play carry animation
        if (state)
        {
            IsInteractBusy = true;
        }
        else
        {
            IsInteractBusy = false;
        }
    }

    private void UpdateCarriedFriendPosition()
    {
        if (Runner.TryFindObject(CarriedFriendId, out var obj))
        {
            // Snap position to above our head
            obj.transform.position = transform.position + Vector3.up * 1.1f;
        }
    }

    public void SetResetAnimation(bool o)
    {
        resetAnimation = o;
    }

    // Raycast
    private void CheckGround()
    {
        LayerMask mask = LayerMask.GetMask("Ground", "Platform");
        LayerMask waterMask = LayerMask.GetMask("WaterSurface");
        IsGrounded = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, mask);
        isWaterSurface = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, waterMask);
        IsInAir = !IsGrounded;

        if (IsGrounded)
        {
            isOptional = false;
            FallingBusy = false;
            if (!IsInteractBusy)
            {
                if (resetAnimation)
                {
                    cAnimation.ReturnToBlendAnimation();

                    if (!IsCarrying)
                    {
                        IsInteractBusy = false;
                    }

                    resetAnimation = false;
                }
            }
        }

        if (isWaterSurface)
        {

        }

        if (IsInAir && rb2D.linearVelocity.y < 0 && !FallingBusy)
        {
            FallingCheck();
            cAnimation.FallingAndFloatAnimation(true);
            resetAnimation = true;
        }
        else
        {
            if (isOptional)
            {
                rb2D.gravityScale = optionalGravity;
            }
            else
            {
                rb2D.gravityScale = normalGravity;
            }
        }
    }

    private void FallingCheck()
    {
        float speedPercent = Mathf.Abs(rb2D.linearVelocity.y) / maxGravity;

        rb2D.gravityScale = Mathf.Lerp(normalGravity, heavyGravity, speedPercent);

        float cappedY = Mathf.Max(rb2D.linearVelocityY, -maxGravity);

        rb2D.linearVelocity = new Vector2(rb2D.linearVelocityX, cappedY);
    }

    protected virtual void OnFixedUpdateSpecific()
    {

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Vector2 start = transform.position;
        Vector2 direction = Vector2.down * rayDistance;
        Gizmos.DrawRay(start, direction);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(start, interactRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(start, playerInteractRadius);
    }
}