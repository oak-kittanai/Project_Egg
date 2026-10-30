using Fusion;
using UnityEngine;

public class BearTrapScript : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private float delayBeforeSnap = 0.5f;
    [SerializeField] private float cooldownTime = 2.4f;
    [Tooltip("เวลาที่ collider ดาเมจเปิดตอนงับ (วินาที) — หลังจากนั้นกับดักที่หุบอยู่ไม่ทำดาเมจ")]
    [SerializeField] private float damageWindow = 0.15f;
    [SerializeField] private ParticleSystem damageParticles;

    [Header("Audio Setting")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bearTrapSoundClip;

    [Networked] private TickTimer CooldownTimer { get; set; }
    [Networked] private TickTimer DelayTimer { get; set; }
    [Networked] private NetworkBool IsTriggered { get; set; }
    [Networked] private NetworkBool HasPlayedFX { get; set; }
    [Networked] private TickTimer DamageWindowTimer { get; set; }
    // นับครั้งที่กับดักทำงาน — ทุกเครื่องเล่นท่างับใหม่เมื่อค่าเปลี่ยน (TP-1)
    [Networked] private int ArmCount { get; set; }

    [SerializeField] Collider2D doDamageColl2D;
    [SerializeField] private Animator trapAnimator;

    private static readonly int TriggerOnState = Animator.StringToHash("Trap_trigger_on");
    private static readonly int TriggerResetState = Animator.StringToHash("Trap_trigger_reset");
    private int shownArmCount;
    private bool shownTriggered;

    // Test
    public bool isFunctionExecuted;

    private void Awake()
    {
        if (doDamageColl2D != null) doDamageColl2D.enabled = false;
    }

    public override void Spawned()
    {
        shownArmCount = ArmCount;
        shownTriggered = IsTriggered;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        if (GameplayPause.Frozen) return; // ไม่ทำงานตอน pause — เดิมยืนบนกับดักตอน pause แล้วกับดักงับเอง (TP-1)

        if (!IsTriggered && other.CompareTag("Player") && CooldownTimer.ExpiredOrNotRunning(Runner))
        {
            IsTriggered = true;
            HasPlayedFX = false;
            ArmCount++;
            DelayTimer = TickTimer.CreateFromSeconds(Runner, delayBeforeSnap);
            CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldownTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // freeze ตอน pause/dialogue — พักเวลาไว้ เล่นต่อแล้วงับ/รีเซ็ตตามเวลาที่เหลือ
        // (เดิม timer หมดระหว่าง pause: งับกับรีเซ็ตเกิดใน tick เดียว ไม่โดนดาเมจ แล้วกับดักค้างท่าหุบ)
        if (GameplayPause.Frozen)
        {
            DelayTimer = GameplayPause.Hold(DelayTimer, Runner);
            CooldownTimer = GameplayPause.Hold(CooldownTimer, Runner);
            DamageWindowTimer = GameplayPause.Hold(DamageWindowTimer, Runner);
            return;
        }

        if (IsTriggered && !HasPlayedFX && DelayTimer.Expired(Runner))
        {
            HasPlayedFX = true;

            if (doDamageColl2D != null)
            {
                // เปิดดาเมจแค่ช่วงงับ (เดิมเปิดค้างจนหมด cooldown — เดินเข้ากับดักที่หุบอยู่ก็โดน)
                doDamageColl2D.enabled = true;
                DamageWindowTimer = TickTimer.CreateFromSeconds(Runner, damageWindow);

                ContactFilter2D filter = new ContactFilter2D();
                filter.SetLayerMask(LayerMask.GetMask("Player"));

                Collider2D[] results = new Collider2D[5];
                int hitCount = doDamageColl2D.Overlap(filter, results);

                if (hitCount > 0)
                {
                    RPC_PlaySnapEffect();
                }
            }
        }

        if (DamageWindowTimer.Expired(Runner))
        {
            DamageWindowTimer = TickTimer.None;
            if (doDamageColl2D != null) doDamageColl2D.enabled = false;
        }

        // รีเซ็ต
        if (IsTriggered && CooldownTimer.Expired(Runner))
        {
            IsTriggered = false;
            HasPlayedFX = false;
            DelayTimer = TickTimer.None;
            DamageWindowTimer = TickTimer.None;

            if (doDamageColl2D != null)
            {
                doDamageColl2D.enabled = false;
            }

            RPC_ResetTrapVisuals();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlaySnapEffect()
    {
        if (damageParticles != null)
        {
            Instantiate(damageParticles, transform.position, Quaternion.identity);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetTrapVisuals()
    {
            trapAnimator.SetBool("Trigger", false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayShootSound()
    {
        AudioManager.PlayObjectSfx(audioSource, bearTrapSoundClip, transform.position);
    }

    public void SetReturnTrigger()
    {
        if (!HasStateAuthority) return;

        isFunctionExecuted = true;
        RPC_PlayShootSound();
    }

    public override void Render()
    {
        if (trapAnimator == null) return;

        // ท่าหยุดกลางคันตอน pause แล้วเล่นต่อพร้อมเกม
        trapAnimator.speed = GameplayPause.Frozen ? 0f : 1f;

        // สั่งท่าตรงจังหวะที่ host ตั้ง — เดิมรอ exit time ของ controller อย่างเดียว:
        // หมด cooldown แล้วโดนเหยียบซ้ำทันที Trigger เป็น false แค่ 1 tick animator ไม่ทันออกจากท่าหุบ ค้างอยู่อย่างนั้น
        if (ArmCount != shownArmCount) trapAnimator.Play(TriggerOnState, 0, 0f);
        else if (shownTriggered && !IsTriggered) trapAnimator.Play(TriggerResetState, 0, 0f);
        shownArmCount = ArmCount;
        shownTriggered = IsTriggered;

        trapAnimator.SetBool("Trigger", IsTriggered);
    }
}