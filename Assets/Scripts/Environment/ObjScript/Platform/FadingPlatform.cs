using UnityEngine;
using Fusion;
public class FadingPlatform : NetworkBehaviour
{
    [Header("Fading Setting")]
    [Tooltip("ความเร็วจาง")]
    [SerializeField] float fadeSpeed = 2.0f;
    [Range(0f, 1f)]
    [SerializeField] float minOpacity = 0f; 

    private SpriteRenderer sr;

    //=== ตัวแปร Network ===
    [Networked] private int PlayersCount { get; set; }
    [Networked] private float TargetAlpha { get; set; }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            TargetAlpha = 1.0f;
            PlayersCount = 0;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        TargetAlpha = (PlayersCount > 0) ? minOpacity : 1.0f;
    }

    public override void Render()
    {
        if (!Mathf.Approximately(sr.color.a, TargetAlpha))
        {
            Color currentColor = sr.color;
            currentColor.a = Mathf.MoveTowards(currentColor.a, TargetAlpha, fadeSpeed * Time.deltaTime);
            sr.color = currentColor;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.transform.position.y > transform.position.y)
            {
                PlayersCount++;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayersCount = Mathf.Max(0, PlayersCount - 1);
        }
    }
}