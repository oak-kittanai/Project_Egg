using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Fusion;
using Assert = NUnit.Framework.Assert;

// Integration tests สำหรับฟีเจอร์ "ผู้เล่นยืนบน moving platform แล้วถูกพาไปด้วย" (player-driven follow)
// - แพเลื่อนแนวนอน: ผู้เล่นต้องติดหนึบตาม x ของแพ (ไม่ลื่นหลุด)
// - แพเลื่อนแนวตั้ง: ผู้เล่นต้องอยู่บนแพ ไม่โดนกดทะลุลง (บั๊กเดิม double-count)
// - กลางอากาศ/ไม่ได้อยู่บนแพ: currentPlatform ต้องเป็น null (follow ไม่ทำงาน)
//
// หมายเหตุ: ข้ามการเช็คฝั่ง client-predicted smoothness (ต้อง 2 เครื่อง) ตามที่ตกลงไว้
public class MovingPlatformIntegrationTests
{
    private const string gameManagerPath = "Prefabs/GameManager";
    private const string platformPath = "Prefabs/Object_Prefabs/ScriptObject/moving_platfrom";
    private const string duckPath = "Prefabs/Character_Prefabs/Player(Duck)";

    private NetworkRunner runner;
    private NetworkObject gmObj;
    private NetworkObject platObj;
    private NetworkObject playerObj;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        var runnerObj = new GameObject("TestRunner");
        runner = runnerObj.AddComponent<NetworkRunner>();
        runner.ProvideInput = false;
        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();

        // ต้องมี RunnerSimulatePhysics2D ที่ตั้งค่าตรงกับเกม (prefab NetworkRunner) ก่อน StartGame
        // ไม่งั้น Fusion auto-add แบบ default แล้ว physics ของแพไม่ถูก step ในซีนที่ player raycast
        // -> CheckGround หาแพไม่เจอ -> IsGrounded เป็น false ตลอด
        AddGamePhysicsRunner(runnerObj);

        var startTask = runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Single,
            SessionName = "PlatformTest_" + System.Guid.NewGuid(),
            SceneManager = sceneManager,
        });
        yield return new WaitUntil(() => startTask.IsCompleted);
        Assert.IsTrue(startTask.Result.Ok, $"NetworkRunner start failed: {startTask.Result.ShutdownReason}");

        // GameManager จำเป็นต่อ MovementCharacter (ไม่งั้น FUN early-return)
        var gmPrefab = Resources.Load<NetworkObject>(gameManagerPath);
        Assert.IsNotNull(gmPrefab, $"ไม่พบ prefab ที่ Resources/{gameManagerPath}");
        gmObj = runner.Spawn(gmPrefab, Vector3.zero, Quaternion.identity);
        yield return null;

        Assert.IsNotNull(GameManager.Instance, "GameManager.Instance ต้องถูกตั้งหลัง spawn");
        // เปิด gate การเคลื่อนที่ของผู้เล่น
        GameManager.Instance.isLoadMapDone = true;
        GameManager.Instance.IsGameReady = true;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        if (runner != null && runner.IsRunning)
        {
            if (playerObj != null) runner.Despawn(playerObj);
            if (platObj != null) runner.Despawn(platObj);
            if (gmObj != null) runner.Despawn(gmObj);
            yield return null;
        }
        if (runner != null)
        {
            runner.Shutdown();
            yield return new WaitUntil(() => runner.IsShutdown);
            UnityEngine.Object.DestroyImmediate(runner.gameObject);
        }
        playerObj = platObj = gmObj = null;
    }

    // ───────────────────────── helpers ─────────────────────────

    // เพิ่ม RunnerSimulatePhysics2D ผ่าน reflection (เลี่ยงการอ้าง assembly Fusion.Addons.Physics ตรงๆ ใน asmdef)
    // ตั้งค่าให้ตรงกับ prefab NetworkRunner ของเกม
    private static void AddGamePhysicsRunner(GameObject runnerObj)
    {
        Type physType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("Fusion.Addons.Physics.RunnerSimulatePhysics2D"))
            .FirstOrDefault(t => t != null);
        Assert.IsNotNull(physType, "หา type Fusion.Addons.Physics.RunnerSimulatePhysics2D ไม่เจอ");

        var phys = runnerObj.AddComponent(physType);
        SetField(phys, "_physicsAuthority", 2);
        SetField(phys, "_physicsTiming", 3);
        SetField(phys, "ClientPhysicsSimulation", 3);
        SetField(phys, "DeltaTimeMultiplier", 1f);
        SetField(phys, "SetUnityFixedTimestep", true);
    }

    // set field แบบ enum-safe (รองรับทั้ง enum, int, float, bool)
    private static void SetField(object obj, string field, object intLikeValue)
    {
        var t = obj.GetType();
        FieldInfo f = null;
        while (f == null && t != null) { f = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance); t = t.BaseType; }
        if (f == null) { Debug.LogWarning($"[Test] หา field '{field}' บน {obj.GetType().Name} ไม่เจอ (ข้าม)"); return; }

        object value = f.FieldType.IsEnum
            ? Enum.ToObject(f.FieldType, Convert.ToInt32(intLikeValue))
            : Convert.ChangeType(intLikeValue, f.FieldType);
        f.SetValue(obj, value);
    }

    private static void ForceGameReady()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isLoadMapDone = true;
            GameManager.Instance.IsGameReady = true;
        }
    }

    private static T GetPrivate<T>(object obj, string field) where T : class
    {
        var f = obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        // field อาจอยู่ใน base class (เช่น currentPlatform อยู่บน MovementCharacter)
        var t = obj.GetType();
        while (f == null && t.BaseType != null) { t = t.BaseType; f = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance); }
        Assert.IsNotNull(f, $"หา field '{field}' ใน {obj.GetType().Name} ไม่เจอ");
        return f.GetValue(obj) as T;
    }

    private static void SetPrivate(object obj, string field, object value)
    {
        var t = obj.GetType();
        FieldInfo f = null;
        while (f == null && t != null) { f = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance); t = t.BaseType; }
        Assert.IsNotNull(f, $"หา field '{field}' ใน {obj.GetType().Name} ไม่เจอ");
        f.SetValue(obj, value);
    }

    // เสกแพ + ตั้งค่าการเคลื่อนที่แบบนุ่มๆ (กันแพพุ่งแรงตอนเทสต์) แล้วรอให้เข้า sine motion
    private IEnumerator SpawnPlatform(bool isVertical)
    {
        var platPrefab = Resources.Load<NetworkObject>(platformPath);
        Assert.IsNotNull(platPrefab, $"ไม่พบ prefab ที่ Resources/{platformPath}");
        platObj = runner.Spawn(platPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);

        var plat = platObj.GetComponent<MovingPlateform>();
        Assert.IsNotNull(plat, "platform prefab ต้องมี MovingPlateform");
        SetPrivate(plat, "speed", 1f);
        SetPrivate(plat, "distance", 2f);
        SetPrivate(plat, "isVertical", isVertical);

        for (int i = 0; i < 5; i++) yield return null; // เข้าสู่การเคลื่อนที่ steady
    }

    private MovementCharacter SpawnDuckAt(Vector3 respawn)
    {
        GameManager.Instance.respawnPos = respawn; // ผู้เล่นจะ teleport มาที่นี่ตอน tick แรก
        var duckPrefab = Resources.Load<NetworkObject>(duckPath);
        Assert.IsNotNull(duckPrefab, $"ไม่พบ prefab ที่ Resources/{duckPath}");
        playerObj = runner.Spawn(duckPrefab, respawn, Quaternion.identity);
        return playerObj.GetComponent<MovementCharacter>();
    }

    // ───────────────────────── tests ─────────────────────────

    [UnityTest]
    public IEnumerator HorizontalPlatform_PlayerTracksPlatformX()
    {
        yield return SpawnPlatform(isVertical: false);
        Vector3 pPos = platObj.transform.position;

        // ลอยเหนือแพ ~1.0 (ไม่ทับ collider แต่ raycast ลงเจอ) แล้ว freeze Y กันร่วง
        // -> แยกทดสอบเฉพาะ "horizontal follow" ให้ deterministic
        MovementCharacter player = SpawnDuckAt(pPos + Vector3.up * 1.0f);
        player.rb2D.constraints |= RigidbodyConstraints2D.FreezePositionY;

        for (int i = 0; i < 20; i++) { ForceGameReady(); yield return null; } // teleport + settle

        Debug.Log($"[DIAG] setup: platformPos={platObj.transform.position} (start was {pPos}) " +
                  $"playerPos={player.transform.position} grounded={player.IsGrounded} gameReady={GameManager.Instance.IsGameReady}");
        Assert.IsTrue(player.IsGrounded, "ผู้เล่นควร grounded (raycast เจอแพด้านล่าง)");
        Transform detected = GetPrivate<Transform>(player, "currentPlatform");
        Assert.AreEqual(platObj.transform, detected, "currentPlatform ต้องเป็นแพที่ยืนอยู่");

        float offset0 = player.transform.position.x - platObj.transform.position.x;
        float platStartX = platObj.transform.position.x;
        float platMinX = platStartX, platMaxX = platStartX;
        float worstDrift = 0f;

        for (int i = 0; i < 60; i++)
        {
            ForceGameReady();
            yield return null;

            float px = platObj.transform.position.x;
            platMinX = Mathf.Min(platMinX, px);
            platMaxX = Mathf.Max(platMaxX, px);

            float offset = player.transform.position.x - px;
            float drift = Mathf.Abs(offset - offset0);
            worstDrift = Mathf.Max(worstDrift, drift);

            if (i % 10 == 0)
                Debug.Log($"[DIAG] t{i}: platX={px:F2} playerX={player.transform.position.x:F2} drift={drift:F3} grounded={player.IsGrounded}");
        }

        Assert.Greater(platMaxX - platMinX, 0.3f, "แพต้องเลื่อนแนวนอนจริง (ไม่งั้นเทสต์ไม่มีความหมาย)");
        Assert.Less(worstDrift, 0.1f,
            $"ผู้เล่นต้องติดหนึบกับแพแนวนอน (offset คงที่) แต่ drift สูงสุด={worstDrift:F3} = ลื่นหลุด/ไม่ตามแพ");
    }

    [UnityTest]
    public IEnumerator VerticalPlatform_PlayerRidesUpDown_NotPushedDown()
    {
        yield return SpawnPlatform(isVertical: true);
        Vector3 pPos = platObj.transform.position;
        Collider2D platCol = platObj.GetComponent<Collider2D>();
        Assert.IsNotNull(platCol, "platform ต้องมี Collider2D");
        float platHalfH = platCol.bounds.extents.y;

        // ปล่อยให้เป็ดตกลงไปนั่งบนแพจริงผ่าน collision; freeze X กันไถลแนวนอน
        MovementCharacter player = SpawnDuckAt(new Vector3(pPos.x, pPos.y + platHalfH + 0.6f, 0f));
        player.rb2D.constraints |= RigidbodyConstraints2D.FreezePositionX;

        for (int i = 0; i < 45; i++) { ForceGameReady(); yield return null; } // teleport + ตก + นั่งบนแพ

        Assert.IsTrue(player.IsGrounded, "ผู้เล่นควรนั่งบนแพ (grounded)");

        float seatedOffset = player.transform.position.y - platObj.transform.position.y;
        float platStartY = platObj.transform.position.y;
        float platMinY = platStartY, platMaxY = platStartY;
        float minTopMargin = float.MaxValue;   // player.y - platformTop ต่ำสุด (ต้องไม่ติดลบ = ไม่โดนกดทะลุ)
        float worstFollow = 0f;

        for (int i = 0; i < 90; i++)
        {
            ForceGameReady();
            yield return null;

            float platY = platObj.transform.position.y;
            platMinY = Mathf.Min(platMinY, platY);
            platMaxY = Mathf.Max(platMaxY, platY);

            float topMargin = player.transform.position.y - (platY + platHalfH);
            minTopMargin = Mathf.Min(minTopMargin, topMargin);

            float follow = Mathf.Abs((player.transform.position.y - platY) - seatedOffset);
            worstFollow = Mathf.Max(worstFollow, follow);

            if (i % 15 == 0)
                Debug.Log($"[DIAG] t{i}: platY={platY:F2} playerY={player.transform.position.y:F2} topMargin={topMargin:F3} follow={follow:F3} grounded={player.IsGrounded}");
        }

        Assert.Greater(platMaxY - platMinY, 0.3f, "แพต้องเลื่อนแนวตั้งจริง");
        Assert.Greater(minTopMargin, -0.05f,
            $"ผู้เล่นต้องอยู่บนผิวแพเสมอ ไม่โดนกดทะลุลง (บั๊กเดิม) แต่ทะลุลงลึกสุด={minTopMargin:F3}");
        Assert.Less(worstFollow, 0.35f,
            $"ผู้เล่นต้องขึ้น-ลงตามแพ แต่ระยะคลาดสูงสุด={worstFollow:F3}");
    }

    [UnityTest]
    public IEnumerator PlayerNotOverPlatform_HasNoCurrentPlatform()
    {
        // ไม่เสกแพ — ผู้เล่นอยู่กลางอากาศ
        MovementCharacter player = SpawnDuckAt(new Vector3(0f, 50f, 0f));

        for (int i = 0; i < 12; i++) { ForceGameReady(); yield return null; }

        Transform detected = GetPrivate<Transform>(player, "currentPlatform");
        Assert.IsNull(detected, "อยู่กลางอากาศ ไม่ควรมี currentPlatform");
        Assert.IsFalse(player.IsGrounded, "อยู่กลางอากาศ ไม่ควร grounded");
    }
}
