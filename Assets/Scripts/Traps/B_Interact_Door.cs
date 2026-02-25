using UnityEngine;

public class B_Interact_Door : MonoBehaviour
{
    [Header("Target Door")]
    [SerializeField] Interact_Door targetDoor;

    [Header("Visual Feedback")]
    [SerializeField] Sprite unpressed;
    [SerializeField] Sprite pressed;

    private SpriteRenderer spriteRenderer;
    private int objectsOnButton = 0;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (unpressed != null) spriteRenderer.sprite = unpressed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Box"))
        {
            objectsOnButton++;
            UpdateStatus();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Box"))
        {
            objectsOnButton--;
            UpdateStatus();
        }
    }

    private void UpdateStatus()
    {
        bool isDown = objectsOnButton > 0;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = isDown ? pressed : unpressed;
        }

        if (targetDoor != null)
        {
            targetDoor.SetDoorState(isDown);
        }
    }
}