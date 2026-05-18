using Fusion;
using UnityEngine;

public class NetworkSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab ที่ต้องการเสก (วัตถุนี้ต้องแปะคอมโพเนนต์ NetworkObject อยู่ด้วย)")]
    [SerializeField] private NetworkObject prefabToSpawn;

    [Tooltip("ระยะเวลาหน่วงในการเสกแต่ละรอบ (วินาที)")]
    [SerializeField] private float spawnInterval = 4.0f;

    [Tooltip("จุดเกิดของ Object (หากปล่อยว่างไว้ วัตถุจะเกิดที่ตำแหน่งของตัว Spawner เอง)")]
    [SerializeField] private Transform spawnPoint;

    // --- ตัวแปรสำหรับคุมเวลาบน Network ---
    [Networked] private TickTimer SpawnCooldownTimer { get; set; }

    public override void Spawned()
    {
        // เริ่มต้นตั้งเวลาถอยหลังสำหรับการเสกชิ้นแรก (ให้สิทธิ์เฉพาะ Host เป็นคนตั้งต้น)
        if (HasStateAuthority)
        {
            ResetTimer();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // ⚠️ ข้อควรระวังเรื่อง Network 1:
        // บังคับให้เฉพาะ Host (State Authority) เท่านั้นที่มีสิทธิ์คำนวณเวลาและสั่งเสก
        // ห้ามปล่อยให้ Client สั่งเสกเองเด็ดขาด ไม่งั้นวัตถุจะเกิดเบิ้ล ซ้ำซ้อน หรือหลุด Sync ทันที
        if (!HasStateAuthority) return;

        // เช็คว่าเวลาคูลดาวน์ถอยหลังหมดลงหรือยัง
        if (SpawnCooldownTimer.Expired(Runner))
        {
            ExecuteSpawn();
            ResetTimer(); // รีเซ็ตเวลารอสำหรับรอบถัดไป
        }
    }

    private void ExecuteSpawn()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[{name}] กรุณาลาก Prefab ที่ต้องการเสกมาใส่ใน Inspector ก่อนใช้งาน!");
            return;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        // ⚠️ ข้อควรระวังเรื่อง Network 2:
        // ในระบบ Network เราจะไม่ใช้ Instantiate ของ Unity แต่ต้องใช้ Runner.Spawn
        // เพื่อให้วัตถุนี้ถูกสร้างขึ้นมาและซิงค์ข้อมูลตำแหน่ง/สเตทไปยังหน้าจอของผู้เล่นทุกคนพร้อมกัน
        Runner.Spawn(prefabToSpawn, spawnPos, spawnRot, Object.InputAuthority);
    }

    private void ResetTimer()
    {
        // ⚠️ ข้อควรระวังเรื่อง Network 3:
        // เราจะไม่ใช้ Coroutine หรือ Time.deltaTime ของ Unity ใน FixedUpdateNetwork
        // แต่จะใช้ TickTimer ของ Fusion เพื่อให้นับเวลาได้อย่างแม่นยำตามอัตรา Tick ของเซิร์ฟเวอร์
        // และรองรับระบบตรวจสอบความถูกต้องย้อนหลัง (Rollback) ได้อย่างไร้รอยต่อ
        SpawnCooldownTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
    }
}