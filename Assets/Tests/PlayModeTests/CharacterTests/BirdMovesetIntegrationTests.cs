using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Fusion;

public class BirdMovesetIntegrationTests
{
    private string prefabsPath = "Prefabs/Character_Prefabs/Player(Bird).prefab";

    private GameObject birdObject;
    private Bird_Moveset birdMoveset;

    [SetUp]
    public void Setup()
    {
        birdObject = GameObject.Find(prefabsPath);

        birdObject.GetComponentInChildren<Rigidbody2D>();
        birdObject.GetComponentInChildren<LineRenderer>();

        birdMoveset = birdObject.GetComponent<Bird_Moveset>();

    }

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
        // ARRANGE
        // ==========================================
        birdMoveset.IsFlying = true;
        birdMoveset.AlreadyFloating = true;
        birdMoveset.IsAlreadyFly = true;

        // ==========================================
        // ACT
        // ==========================================
        birdMoveset.ForceCancelFlight();

        // ปล่อยให้เวลาใน Unity เดินหน้าไป 1 เฟรม เพื่อให้สคริปต์ประมวลผล
        yield return null;

        // ==========================================
        // ASSERT
        // ==========================================
        Assert.IsFalse(birdMoveset.IsFlying, "Error: นกควรจะหยุดบินแล้ว (IsFlying ต้องเป็น false)");
        Assert.IsFalse(birdMoveset.AlreadyFloating, "Error: นกควรจะเลิกลอยตัวแล้ว (AlreadyFloating ต้องเป็น false)");
        Assert.IsFalse(birdMoveset.IsAlreadyFly, "Error: สถานะ IsAlreadyFly ต้องถูกรีเซ็ตเป็น false");
    }
}