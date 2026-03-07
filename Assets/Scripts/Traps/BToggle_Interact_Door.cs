using Fusion;
using UnityEngine;

public class BToggle_Interact_Door : NetworkBehaviour
{
    [Header("Target Door")]
    [SerializeField] Interact_Door targetDoor;

    [Header("Visual Feedback")]
    [SerializeField] Sprite unpressed;
    [SerializeField] Sprite pressed;

    private SpriteRenderer spriteRenderer;
    private bool isToggled = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (unpressed != null) spriteRenderer.sprite = unpressed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Player Check
        if (collision.CompareTag("Player") || collision.CompareTag("Box"))
        {
            isToggled = true;
            UpdateState();
        }
    }

    private void UpdateState()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = isToggled ? pressed : unpressed;
        }

        if (targetDoor != null)
        {
            targetDoor.SetDoorState(isToggled);
        }
    }
}
