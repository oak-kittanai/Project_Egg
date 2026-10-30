using Fusion;

// ของในด่านตอน pause/dialogue (TP-1..TP-7)
// pause ไม่ได้หยุด runner: tick ยังเดิน, TickTimer ยังนับ, physics/trigger และ Animator ยังทำงาน
// FixedUpdateNetwork ที่แค่ return เลยหยุดไม่ครบ — timer หมดระหว่าง pause แล้วทำงานทันทีที่เล่นต่อ
public static class GameplayPause
{
    // GameplayFrozen ที่เรียกจาก Render ได้ (GameManager อาจยังไม่ Spawned — ห้ามอ่าน [Networked] ก่อน Spawned)
    public static bool Frozen
    {
        get
        {
            GameManager gm = GameManager.Instance;
            return gm != null && gm.Object != null && gm.Object.IsValid && gm.IsGameplayFrozen;
        }
    }

    // เรียกทุก tick ที่ freeze: เลื่อนเวลาหมดออกไป 1 tick ให้เวลาที่เหลือคงเดิม — เล่นต่อแล้วนับต่อจากเดิม
    // timer ที่ไม่ได้เริ่มหรือหมดไปแล้วคืนค่าเดิม
    public static TickTimer Hold(TickTimer timer, NetworkRunner runner)
    {
        if (!timer.IsRunning || timer.Expired(runner)) return timer;

        int remaining = timer.RemainingTicks(runner) ?? 0;
        return TickTimer.CreateFromTicks(runner, remaining + 1);
    }
}
