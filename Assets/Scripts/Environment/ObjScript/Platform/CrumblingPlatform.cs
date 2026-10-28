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
    [Tooltip("ความยาวท่าแตก (วินาที) — sprite จะถูกซ่อนหลังเล่นจบ")]
    [SerializeField] float breakAnimDuration = 0.875f;

    [Networked] public PlatformState CurrentState { get; set; }
    [Networked] private TickTimer StateTimer { get; set; }

    private BoxCollider2D boxCollider;
    private SpriteRenderer sr;
    private Animator animator;

    private PlatformState lastState;
    private Vector3 originalPos;
    private float hideSpriteAtTime = -1f;

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
        if (GameplayPause.Frozen) return;

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

        // freeze ตอน pause/dialogue/tutorial — เดิมไม่เช็คเลย: แท่นที่สั่นอยู่พังระหว่าง pause คนบนแท่นลอยค้างแล้วตกตอนเล่นต่อ (TP-4)
        if (GameplayPause.Frozen)
        {
            StateTimer = GameplayPause.Hold(StateTimer, Runner);
            return;
        }

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
        // ตอน pause: ท่าแตกหยุดกลางคัน, ไม่สั่น, เลื่อนเวลาซ่อน sprite ออกไปเท่าเวลาที่หยุด
        bool frozen = GameplayPause.Frozen;
        if (animator != null) animator.speed = frozen ? 0f : 1f;
        if (frozen && hideSpriteAtTime >= 0f) hideSpriteAtTime += Time.deltaTime;

        if (lastState != CurrentState)
        {
            if (CurrentState == PlatformState.Broken)
            {
                // เล่นท่าแตกให้เห็นก่อน แล้วค่อยซ่อน sprite เมื่อ clip จบ
                // (เดิมซ่อนทันทีพร้อมสั่งเล่น จึงไม่เห็นท่าแตก แต่ไปเห็นเฟรมแตกค้างตอนโผล่กลับมา)
                if (animator != null) animator.Play("Platform_Break", 0, 0f);
                hideSpriteAtTime = Time.time + breakAnimDuration;
                // ปิด collider บน "ทุก peer" ไม่ใช่แค่ state authority — ไม่งั้นฝั่ง client
                // ที่ไม่ได้รัน BreakPlatform() จะยังชนพื้นที่พังไปแล้ว (collider ไม่ sync ข้ามเครื่อง)
                if (boxCollider != null) boxCollider.enabled = false;

                transform.position = originalPos;
            }
            else if (CurrentState == PlatformState.Idle)
            {
                hideSpriteAtTime = -1f;
                if (animator != null) animator.Play("Platform_Idle", 0, 0f);
                if (sr != null) sr.enabled = true;
                if (boxCollider != null) boxCollider.enabled = true;
            }

            lastState = CurrentState;
        }

        if (hideSpriteAtTime >= 0f && Time.time >= hideSpriteAtTime)
        {
            hideSpriteAtTime = -1f;
            if (CurrentState == PlatformState.Broken && sr != null) sr.enabled = false;
        }

        if (CurrentState == PlatformState.Shaking && !frozen)
        {
            float t = (float)Runner.SimulationTime * shakeFrequency;
            Vector3 shakeOffset = new Vector3(Mathf.Sin(t), Mathf.Cos(t * 1.3f), 0f) * shakeIntensity;
            transform.position = originalPos + shakeOffset;
        }
    }
}