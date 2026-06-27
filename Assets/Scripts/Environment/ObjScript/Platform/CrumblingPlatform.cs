using Fusion;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class CrumblingPlatform : NetworkBehaviour
{
    public enum PlatformState { Idle, Shaking, Broken }

    [Header("Settings")]
    [SerializeField] float crumbleDelay = 1.0f;
    [SerializeField] float respawnTime = 3.0f;
    [Tooltip("ความแรงในการสั่นก่อนพัง")]
    [SerializeField] float shakeIntensity = 0.05f;
    [Tooltip("ความเร็วในการสั่น")]
    [SerializeField] float shakeFrequency = 25f;

    [Networked] public PlatformState CurrentState { get; set; }
    [Networked] private TickTimer StateTimer { get; set; }

    private BoxCollider2D boxCollider;
    private SpriteRenderer sr;
    private Animator animator;

    private PlatformState lastState;
    private Vector3 originalPos;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public override void Spawned()
    {
        originalPos = transform.position;
        lastState = CurrentState;

        // ตั้ง collider + sprite ให้ตรงกับ state ปัจจุบันตั้งแต่ spawn (รองรับคนเข้าทีหลัง
        // ที่ platform อาจพังไปแล้ว) — boxCollider.enabled เป็น state ของแต่ละเครื่อง ไม่ได้ sync อัตโนมัติ
        bool solid = CurrentState != PlatformState.Broken;
        if (boxCollider != null) boxCollider.enabled = solid;
        if (sr != null) sr.enabled = solid;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        if (CurrentState == PlatformState.Idle && collision.gameObject.CompareTag("Player"))
        {
            if (collision.transform.position.y > transform.position.y)
            {
                CurrentState = PlatformState.Shaking;
                StateTimer = TickTimer.CreateFromSeconds(Runner, crumbleDelay);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (CurrentState == PlatformState.Shaking && StateTimer.Expired(Runner))
        {
            BreakPlatform();
        }
        else if (CurrentState == PlatformState.Broken && StateTimer.Expired(Runner))
        {
            ResetPlatform();
        }
    }

    private void BreakPlatform()
    {
        CurrentState = PlatformState.Broken;
        StateTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);
        boxCollider.enabled = false;
    }

    private void ResetPlatform()
    {
        CurrentState = PlatformState.Idle;
        StateTimer = TickTimer.None;
        boxCollider.enabled = true;
    }

    public override void Render()
    {
        if (lastState != CurrentState)
        {
            if (CurrentState == PlatformState.Broken)
            {
                if (animator != null) animator.SetTrigger("Break");
                if (sr != null) sr.enabled = false;
                // ปิด collider บน "ทุก peer" ไม่ใช่แค่ state authority — ไม่งั้นฝั่ง client
                // ที่ไม่ได้รัน BreakPlatform() จะยังชนพื้นที่พังไปแล้ว (collider ไม่ sync ข้ามเครื่อง)
                if (boxCollider != null) boxCollider.enabled = false;

                transform.position = originalPos;
            }
            else if (CurrentState == PlatformState.Idle)
            {
                if (animator != null) animator.SetTrigger("Reset");
                if (sr != null) sr.enabled = true;
                if (boxCollider != null) boxCollider.enabled = true;
            }

            lastState = CurrentState;
        }

        if (CurrentState == PlatformState.Shaking)
        {
            float t = (float)Runner.SimulationTime * shakeFrequency;
            Vector3 shakeOffset = new Vector3(Mathf.Sin(t), Mathf.Cos(t * 1.3f), 0f) * shakeIntensity;
            transform.position = originalPos + shakeOffset;
        }
    }
}