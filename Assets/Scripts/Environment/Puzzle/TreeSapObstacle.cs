using Fusion;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class TreeSapObstacle : NetworkBehaviour
{
    [Header("Sprite")]
    [SerializeField] private Sprite[] sapSprites;
    [SerializeField] private float hitCooldown = 0.5f; //Coolstar

    [Header("Component")]
    private SpriteRenderer sr;
    private Collider2D col;

    // --- ตัวแปร Network ---
    [Networked] private int CurrentPhase { get; set; }
    [Networked] private NetworkBool IsBroken { get; set; }
    [Networked] private TickTimer CooldownTimer { get; set; }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentPhase = 0;
            IsBroken = false;
        }

        UpdateVisuals();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        if (IsBroken)
        {
            return;
        }

        if (!CooldownTimer.ExpiredOrNotRunning(Runner))
        {
            return;
        }

        if (collision.gameObject.CompareTag("Rock"))
        {
            float impactSpeed = collision.relativeVelocity.magnitude;

            if (impactSpeed < 1f)
            {
                return;
            }

            TakeHit();
        }
    }

    private void TakeHit()
    {
        CooldownTimer = TickTimer.CreateFromSeconds(Runner, hitCooldown);

        CurrentPhase++;

        if (CurrentPhase >= sapSprites.Length - 1)
        {
            CurrentPhase = sapSprites.Length - 1;
            IsBroken = true;
        }
    }

    public override void Render()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (sapSprites == null || sapSprites.Length == 0) return;

        if (CurrentPhase < sapSprites.Length)
        {
            sr.sprite = sapSprites[CurrentPhase];
        }

        if (col != null)
        {
            col.enabled = !IsBroken;
        }
    }
}