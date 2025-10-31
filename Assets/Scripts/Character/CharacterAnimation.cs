using UnityEngine;
using UnityEngine.UIElements;

public class CharacterAnimation : MonoBehaviour
{
    [Header("Referent")]
    Animator animator;
    SpriteRenderer spriteRenderer;

    [Header("Controller Setting")]
    [SerializeField] RuntimeAnimatorController controller_Eagle;
    [SerializeField] RuntimeAnimatorController controller_Duck;

    public void Setup()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        animator.runtimeAnimatorController = controller_Duck;
        /*else if (characterManager.isEagle)
        {
            animator.runtimeAnimatorController = controller_Eagle;
        }
        else Debug.Log("Error can't found Identify");*/
    }

    public void UpdateAnimation(Vector3 direction)
    {
        animator.SetFloat("X", direction.x);

        if (direction.x < -0.01f)
        {
            spriteRenderer.flipX = false;
        }

        if (direction.x > 0.01f)
        {
            spriteRenderer.flipX = true;
        }

        animator.SetFloat("Y", direction.y);
    }

    // jump & skill
    public void UpdateActionAnimation(int i)
    {
        if (i == 1)
        {
            animator.Play("Jump", 0);
        }
        else if (i == 2)
        {
            animator.Play("Fly", 0);
        }

        if (i == 3)
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
