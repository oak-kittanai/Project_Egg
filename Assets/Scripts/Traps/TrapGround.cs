using Fusion;
using System.Collections;
using UnityEngine;

public class TrapGround : NetworkBehaviour
{
    [Header("Animator")]
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer spriteRenderer;

    [Header("Hitbox")]
    [SerializeField] GameObject hitColl;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrapWork();
    }

    private void TrapWork()
    {
        animator.Play("Trap_trigger_on");
        hitColl.SetActive(true);
        StartCoroutine(WaitAfter());
    }

    private void ResetTrap()
    {
        animator.Play("Trap_trigger_reset");
        hitColl.SetActive(false);
    }

    private IEnumerator WaitAfter()
    {
        yield return new WaitForSeconds(1f);
        ResetTrap();
    }
}
