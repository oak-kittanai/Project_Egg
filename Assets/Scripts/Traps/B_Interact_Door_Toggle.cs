using Fusion;
using UnityEngine;

public class B_Interact_Door_Toggle : NetworkBehaviour
{
    [Header("Target Door")]
    [SerializeField] Interact_Door2 targetDoor;

    [Header("Visual Feedback")]
    [SerializeField] Sprite unpressed;
    [SerializeField] Sprite pressed;

    private SpriteRenderer spriteRenderer;
    private bool isAlready = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (unpressed != null) spriteRenderer.sprite = unpressed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isAlready && (collision.CompareTag("Player") || collision.CompareTag("Box")))
        {
            ActivateButton();
        }
    }

    private void ActivateButton()
    {
        isAlready = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = pressed;
        }

        if (targetDoor != null)
        {
            targetDoor.RegistButtonPress();
        }
    }
}