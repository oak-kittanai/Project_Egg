using Fusion;
using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    [Header("Referent")]
    CharacterStats stats;
    Animator animator;
    SpriteRenderer spriteRenderer;

    [Header("Position")]
    [Networked] public Vector3 Direction {  get; set; }
    [Networked] public int Action { get; set; }
    [Networked] public bool FlipX { get; set; }

    [Header("Character Set")]
    [Networked] public bool isDuck { get; set; }

    [Header("Controller Setting")]
    [SerializeField] RuntimeAnimatorController controller_Eagle;
    [SerializeField] RuntimeAnimatorController controller_Duck;

    public void Setup()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        stats = GetComponent<CharacterStats>();

        if (stats.isDuck)
        {
            animator.runtimeAnimatorController = controller_Duck;
        }
        else
        {
            animator.runtimeAnimatorController = controller_Eagle;
        }
    }

    public void UpdateAnimation(Vector3 direction)
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

    // jump & skill
    public void UpdateActionAnimation(int i)
    {
        Action = i;
        if (Action == 1)
        {
            animator.Play("Jump", 0);
        }
        else if (Action == 2)
        {
            animator.Play("Fly", 0);
        }

        if (Action == 3)
        {
            animator.Play("Float_Down", 0);
            animator.SetBool("Falling", true);
        }
    }

    public void OnGroundCheck()
    {
        animator.SetBool("Falling", false);
    }
}
