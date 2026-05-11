using UnityEngine;
using Fusion;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class FadingPlatform : NetworkBehaviour
{
    [Header("Fading Setting")]
    [SerializeField] float fadeSpeed = 2.0f;
    [Range(0f, 1f)]
    [SerializeField] float minOpacity = 0.1f;

    [Header("Respawn Setting")]
    [SerializeField] float respawnDelay = 2.0f; //หน่วงเวลาเกิด

    private SpriteRenderer sr;
    private BoxCollider2D col;

    //=== ตัวแปร Network ===
    [Networked] private int PlayersCount { get; set; }
    [Networked] private NetworkBool IsBroken { get; set; }
    [Networked] private float TargetAlpha { get; set; }
    [Networked] private TickTimer RespawnTimer { get; set; }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
    }

    public override void Spawned()
    {
        TargetAlpha = 1.0f;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (IsBroken)
        {
            if (RespawnTimer.Expired(Runner))
            {
                TargetAlpha = 1.0f;
                IsBroken = false;
                RespawnTimer = TickTimer.None;
            }
            return;
        }

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

        if (HasStateAuthority && !IsBroken && PlayersCount > 0 && Mathf.Approximately(sr.color.a, minOpacity))
        {
            BreakPlatform();
        }

        col.enabled = !IsBroken;
    }

    private void BreakPlatform()
    {
        IsBroken = true;
        PlayersCount = 0;
        TargetAlpha = 0.0f;
        RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);
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

        if (collision.gameObject.CompareTag("Player") && !IsBroken)
        {
            PlayersCount = Mathf.Max(0, PlayersCount - 1);
        }
    }
}