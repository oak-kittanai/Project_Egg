using Fusion;
using UnityEngine;

public class BearTrapScript : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private float delayBeforeSnap = 0.5f;

    [SerializeField] private float cooldownTime = 2.4f;
    [Networked] private TickTimer CooldownTimer { get; set; }
    [Networked] private TickTimer DelayTimer { get; set; }

    [SerializeField] Collider2D doDamageColl2D;

    [Header("Visuals")]
    [SerializeField] private Animator trapAnimator;
    [Networked] private NetworkBool IsTriggered { get; set; }

    private bool localTriggerPredict;
    private float localPredictTimer;

    private void Awake()
    {
        if (doDamageColl2D != null) doDamageColl2D.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        if (other.CompareTag("Player") && CooldownTimer.ExpiredOrNotRunning(Runner))
        {
            // 1. เหยียบปุ๊บ เซ็ตค่าทันทีเพื่อให้ภาพเริ่มเล่น
            IsTriggered = true;

            // 2. เริ่มนับถอยหลังก่อนจะทำดาเมจ
            DelayTimer = TickTimer.CreateFromSeconds(Runner, delayBeforeSnap);

            // 3. เริ่มนับ Cooldown ไปด้วยเลย
            CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldownTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // เช็คจังหวะที่จะเปิด Collider ดาเมจ
        if (IsTriggered && DelayTimer.Expired(Runner))
        {
            if (doDamageColl2D != null && !doDamageColl2D.enabled)
            {
                doDamageColl2D.enabled = true; // งับจริงแล้ว! เปิด Collider
            }
        }

        // เมื่อ Cooldown หมด ให้รีเซ็ตทุกอย่าง
        if (IsTriggered && CooldownTimer.Expired(Runner))
        {
            IsTriggered = false;
            DelayTimer = TickTimer.None;
            if (doDamageColl2D != null) doDamageColl2D.enabled = false; // ปิด Collider รอครั้งต่อไป
        }
    }

    public override void Render()
    {
        if (trapAnimator != null)
        {
            // ใช้ IsTriggered คุม Animation เหมือนเดิม แต่จังหวะงับจะดูแฟร์ขึ้นเพราะมีดีเลย์
            trapAnimator.SetBool("Trigger", IsTriggered);
        }
    }
}