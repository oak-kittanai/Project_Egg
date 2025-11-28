using Fusion;
using UnityEngine;

public class CharacterAnimation : NetworkBehaviour
{
    [Header("Referent")]
    CharacterStats stats;
    Animator animator;
    SpriteRenderer spriteRenderer;

    [Header("Position")]
    [Networked] public Vector3 Direction {  get; set; }
    [Networked] public int Action { get; set; }
    [Networked] public bool FlipX { get; set; }

    [Header("Controller Setting")]
    [SerializeField] public RuntimeAnimatorController DuckController;
    [SerializeField] public RuntimeAnimatorController BirdController;

    private void Awake()
    {
        Setup();
    }

    public override void Spawned()
    {
        UpdateSkin(stats.skinType);
        if (HasStateAuthority)
        {
            
        }
    }

    public void Setup()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stats = GetComponent<CharacterStats>();
    }

    public void UpdateSkin(SkinType skin)
    {
        if (skin == SkinType.Duck)
        {
            animator.runtimeAnimatorController = DuckController;
        }
        else
        {
            animator.runtimeAnimatorController = BirdController;
        }
    }

    public void UpdateFilp()
    {
        spriteRenderer.flipX = FlipX;
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
