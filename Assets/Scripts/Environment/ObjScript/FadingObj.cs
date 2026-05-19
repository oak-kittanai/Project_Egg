using Fusion;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class FadeWhenPlayerInside : NetworkBehaviour
{
    public float targetAlpha = 0f;
    public float fadeSpeed = 8f;

    [Networked] private int PlayersInside { get; set; }

    private SpriteRenderer spriteRenderer;
    private float originalAlpha;

    public override void Spawned()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalAlpha = spriteRenderer.color.a;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (collision.CompareTag("Player"))
        {
            PlayersInside++;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (collision.CompareTag("Player"))
        {
            PlayersInside--;

            if (PlayersInside < 0) PlayersInside = 0;
        }
    }

    public override void Render()
    {
        if (spriteRenderer == null) return;

        float target = (PlayersInside > 0) ? targetAlpha : originalAlpha;

        Color currentColor = spriteRenderer.color;
        currentColor.a = Mathf.Lerp(currentColor.a, target, Runner.DeltaTime * fadeSpeed);
        spriteRenderer.color = currentColor;
    }
}