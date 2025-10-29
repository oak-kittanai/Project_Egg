using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    [Header("Referent")]
    CharacterManager characterManager;
    CharacterStats stats;
    InputControl input;
    CharacterAction action;

    Animator animator;
    SpriteRenderer spriteRenderer;

    [Header("Controller Setting")]
    [SerializeField] RuntimeAnimatorController controller_Eagle;
    [SerializeField] RuntimeAnimatorController controller_Duck;

    public void Setup()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        characterManager = GetComponent<CharacterManager>();

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
    public void UpdateActionAnimation()
    {
        Debug.Log("UpdateActionAnimation");
    }

}
