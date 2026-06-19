using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Fusion;

public class BirdMovesetIntegrationTests
{
    /*private GameObject birdObject;
    private Bird_Moveset birdMoveset;

    // 1. [SetUp] เตรียมฉากก่อนเทส
    [SetUp]
    public void Setup()
    {
        // จำลองการสร้างตัวละครนกขึ้นมาในฉาก
        birdObject = new GameObject("Test_Bird");

        // ใส่ Component ที่ Bird_Moveset ต้องการ
        birdObject.AddComponent<Rigidbody2D>();
        birdObject.AddComponent<LineRenderer>();

        // แปะสคริปต์หลักที่เราจะทำการเทส
        birdMoveset = birdObject.AddComponent<Bird_Moveset>();
    }

    // 2. [TearDown] ทำลายฉากทิ้งหลังเทสเสร็จ (กันผีหลอก)
    [TearDown]
    public void Teardown()
    {
        if (birdObject != null)
        {
            Object.DestroyImmediate(birdObject);
        }
    }

    // 3. [UnityTest] เริ่มรันเทสบน PlayMode
    [UnityTest]
    public IEnumerator ForceCancelFlight_ShouldStopFlying_And_ResetFloating()
    {
        // ==========================================
        // 📌 ARRANGE (จัดเตรียม)
        // ==========================================
        // จำลองสถานการณ์ให้นกกำลังบินและลอยตัวอยู่
        birdMoveset.IsFlying = true;
        birdMoveset.AlreadyFloating = true;
        birdMoveset.IsAlreadyFly = true;

        // ==========================================
        // 🎬 ACT (ลงมือทำ)
        // ==========================================
        // สั่งให้ระบบบังคับยกเลิกการบินทำงาน
        birdMoveset.ForceCancelFlight();

        // ปล่อยให้เวลาใน Unity เดินหน้าไป 1 เฟรม เพื่อให้สคริปต์ประมวลผล
        yield return null;

        // ==========================================
        // 🔎 ASSERT (ตรวจสอบผลลัพธ์)
        // ==========================================
        // เช็คว่าสถานะทุกอย่างถูกจับปิด (false) ตามที่ควรจะเป็นหรือไม่
        Assert.IsFalse(birdMoveset.IsFlying, "Error: นกควรจะหยุดบินแล้ว (IsFlying ต้องเป็น false)");
        Assert.IsFalse(birdMoveset.AlreadyFloating, "Error: นกควรจะเลิกลอยตัวแล้ว (AlreadyFloating ต้องเป็น false)");
        Assert.IsFalse(birdMoveset.IsAlreadyFly, "Error: สถานะ IsAlreadyFly ต้องถูกรีเซ็ตเป็น false");
    }*/
}