using UnityEngine;

[RequireComponent(typeof(Animator))]
public class LifeCycle : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] RuntimeAnimatorController lifeCycle_Bird;
    [SerializeField] RuntimeAnimatorController lifeCycle_Duck;

    Animator animator;

    private const string STATE_EMPTY = "EmptyState";
    private const string STATE_FRIEND_DEATH = "OnFriendDeath";
    private const string STATE_RESPAWN = "OnRespawn";

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    public void Initialize(bool isBird)
    {
        if (animator == null) return;

        animator.runtimeAnimatorController = isBird ? lifeCycle_Bird : lifeCycle_Duck;
        animator.Play(STATE_EMPTY);
    }

    public void PlayOnFriendDeath()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.Play(STATE_FRIEND_DEATH);
    }

    public void PlayOnRespawn()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.Play(STATE_RESPAWN);
    }

    public void PlayEmpty()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.Play(STATE_EMPTY);
    }
}