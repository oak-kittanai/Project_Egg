using Fusion;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class CrumblingPlatform : NetworkBehaviour
{
    public enum PlatformState { Idle, Shaking, Broken }

    [Header("Settings")]
    [SerializeField] float crumbleDelay = 1.0f;

    [SerializeField] float respawnTime = 3.0f;

    [Networked] public PlatformState CurrentState { get; set; }
    [Networked] private TickTimer StateTimer { get; set; }

    private BoxCollider2D boxCollider;
    private SpriteRenderer sr;
    private Animator animator;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        if (CurrentState == PlatformState.Idle && collision.gameObject.CompareTag("Player"))
        {
            if (collision.transform.position.y > transform.position.y)
            {
                CurrentState = PlatformState.Shaking;
                StateTimer = TickTimer.CreateFromSeconds(Runner, crumbleDelay);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (CurrentState == PlatformState.Shaking && StateTimer.Expired(Runner))
        {
            BreakPlatform();
        }
        else if (CurrentState == PlatformState.Broken && StateTimer.Expired(Runner))
        {
            ResetPlatform();
        }
    }

    private void BreakPlatform()
    {
        CurrentState = PlatformState.Broken;
        StateTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);
        animator.SetTrigger("Break");
        boxCollider.enabled = false;
    }

    private void ResetPlatform()
    {
        CurrentState = PlatformState.Idle;
        StateTimer = TickTimer.None;
        animator.SetTrigger("Reset");
        boxCollider.enabled = true;
    }

    public override void Render()
    {
        if (CurrentState == PlatformState.Broken)
        {
            animator.SetTrigger("Break");
            sr.enabled = false;
        }
        else
        {
            sr.enabled = true;
        }
    }
}