using UnityEngine;
using Fusion;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(LineRenderer))]
public class JellyfishTrap : NetworkBehaviour
{
    public enum JellyState
    {
        Idle,
        Charging,
        Exploding,
        Hidden
    }

    [Header("Settings")]
    [SerializeField] float explosionRadius = 2.5f;
    [SerializeField] float chargeTime = 1.5f;
    [SerializeField] float respawnTime = 3.0f;
    [SerializeField] int damageAmount = 1;
    [SerializeField] float knockbackForce = 5f;
    [SerializeField] Vector3 scaleUpSize = new Vector3(1.2f, 1.2f, 1f);

    [Header("Visuals")]
    [SerializeField] int circleSegments = 50;
    [SerializeField] float explosionAnimDuration = 0.8f;

    [Header("Layer Mask")]
    [SerializeField] LayerMask targetLayer;

    [Networked] public JellyState CurrentState { get; set; }
    [Networked] public TickTimer StateTimer { get; set; }

    private Animator anim;
    private SpriteRenderer sr;
    private Collider2D col;
    private LineRenderer lineRenderer;
    private Vector3 originalScale;
    private Vector3 targetScale;

    private JellyState _prevState;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        lineRenderer = GetComponent<LineRenderer>();

        originalScale = transform.localScale;
        targetScale = Vector3.Scale(originalScale, scaleUpSize);

        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentState = JellyState.Idle;
        }
        _prevState = CurrentState;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (CurrentState == JellyState.Idle && IsTarget(collision))
        {
            CurrentState = JellyState.Charging;
            StateTimer = TickTimer.CreateFromSeconds(Runner, chargeTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        switch (CurrentState)
        {
            case JellyState.Charging:
                if (StateTimer.Expired(Runner))
                {
                    CheckDamage();
                    CurrentState = JellyState.Exploding;
                    StateTimer = TickTimer.CreateFromSeconds(Runner, explosionAnimDuration);
                }
                break;

            case JellyState.Exploding:
                if (StateTimer.Expired(Runner))
                {
                    CurrentState = JellyState.Hidden;
                    StateTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);
                }
                break;

            case JellyState.Hidden:
                if (StateTimer.Expired(Runner))
                {
                    CurrentState = JellyState.Idle;
                    StateTimer = TickTimer.None;
                }
                break;
        }
    }

    public override void Render()
    {
        if (_prevState != CurrentState)
        {
            if (CurrentState == JellyState.Charging) anim.SetTrigger("Charge");
            if (CurrentState == JellyState.Exploding) anim.SetTrigger("Explode");
            if (CurrentState == JellyState.Idle) anim.SetTrigger("Reset");

            bool isVisible = CurrentState != JellyState.Hidden;
            sr.enabled = isVisible;
            col.enabled = isVisible;

            if (CurrentState != JellyState.Charging)
            {
                ClearCircle();
                transform.localScale = (CurrentState == JellyState.Idle) ? originalScale : targetScale;
            }

            _prevState = CurrentState;
        }

        if (CurrentState == JellyState.Charging && StateTimer.IsRunning)
        {
            float timeRemaining = StateTimer.RemainingTime(Runner) ?? 0f;
            float progress = 1f - (timeRemaining / chargeTime);
            progress = Mathf.Clamp01(progress);

            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);

            float currentRadius = Mathf.Lerp(0f, explosionRadius, progress);
            DrawCircle(currentRadius);
        }
    }

    void CheckDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayer);

        foreach (var hit in hits)
        {
            MovementCharacter[] allCharacterMovement = hit.GetComponents<MovementCharacter>();

            foreach (var character in allCharacterMovement)
            {
                if (character.enabled)
                {
                    float pushDirectionX = Mathf.Sign(hit.transform.position.x - transform.position.x);
                    Vector2 knockbackDirection = new Vector2(pushDirectionX, 1f).normalized;

                    character.TakeDamage(damageAmount, knockbackForce, knockbackDirection);

                    Debug.Log($"Do damage To {character.name}: - {damageAmount} hp");
                }
            }
        }
    }


    bool IsTarget(Collider2D collision)
    {
        return ((1 << collision.gameObject.layer) & targetLayer) != 0;
    }

    void DrawCircle(float radius)
    {
        if (radius <= 0)
        {
            ClearCircle();
            return;
        }

        lineRenderer.positionCount = circleSegments + 1;
        float angle = 0f;

        for (int i = 0; i < circleSegments + 1; i++)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            angle += (360f / circleSegments);
        }
    }

    void ClearCircle()
    {
        lineRenderer.positionCount = 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}