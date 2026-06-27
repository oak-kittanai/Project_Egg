using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Fusion;
using Assert = NUnit.Framework.Assert;

// Integration test สำหรับบั๊ก: ทุบ BreakableRock แล้วเล่น animation จบ -> sprite "กลับไปเป็นอันแรก"
// แทนที่จะค้างเป็น sprite ตอนแตก (alreadyBreakRock)
//
// เทสต์นี้ spawn หินจริงผ่าน NetworkRunner แล้วเล่น break animation จริง (controller + clip +
// ChangeSprite Animation Event ของจริง) แล้วเช็คว่า "จบ animation แล้ว sprite ค้างที่อันแตกจริงไหม"
// - ถ้าบั๊กยังอยู่ (event ไม่ยิง หรือ animator ขับ sprite ทับ) เทสต์จะ FAIL ตามอาการที่รายงาน
// - เมื่อแก้ถูกแล้วเทสต์จะ PASS (ใช้เป็น regression test ต่อไป)
public class BreakableRockIntegrationTests
{
    private const string rockPrefabPath = "Prefabs/Object_Prefabs/ScriptObject/BreakAbleRock";

    private NetworkRunner runner;
    private NetworkObject rockNetObj;
    private BreakableRock rock;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        // ต้องมี NetworkRunner จริงก่อน ไม่งั้น [Networked] property ของ BreakableRock จะ throw
        var runnerObj = new GameObject("TestRunner");
        runner = runnerObj.AddComponent<NetworkRunner>();
        runner.ProvideInput = false;
        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();

        var startArgs = new StartGameArgs
        {
            GameMode = GameMode.Single, // เล่นคนเดียวในเครื่อง ไม่ต้องต่อ Photon Cloud
            SessionName = "BreakRockTest_" + System.Guid.NewGuid(),
            SceneManager = sceneManager,
        };

        var startTask = runner.StartGame(startArgs);
        yield return new WaitUntil(() => startTask.IsCompleted);
        Assert.IsTrue(startTask.Result.Ok, $"NetworkRunner start failed: {startTask.Result.ShutdownReason}");

        NetworkObject rockPrefab = Resources.Load<NetworkObject>(rockPrefabPath);
        Assert.IsNotNull(rockPrefab, $"ไม่พบ prefab ที่ Resources/{rockPrefabPath}");

        rockNetObj = runner.Spawn(rockPrefab, Vector3.zero, Quaternion.identity);
        yield return null; // รอ 1 tick ให้ Spawned() รันจบ

        rock = rockNetObj.GetComponent<BreakableRock>();
        Assert.IsNotNull(rock, "Spawn แล้วไม่มี BreakableRock");
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        if (runner != null && runner.IsRunning)
        {
            if (rockNetObj != null) runner.Despawn(rockNetObj);
            yield return null;
        }
        if (runner != null)
        {
            runner.Shutdown();
            yield return new WaitUntil(() => runner.IsShutdown);
            Object.DestroyImmediate(runner.gameObject);
        }
    }

    [UnityTest]
    public IEnumerator PlayBreakAnimation_EndsOnBrokenSprite_NotBackToOriginal()
    {
        // อ่าน field ที่ต้องใช้จาก BreakableRock (เป็น [SerializeField] private -> reflection)
        Sprite brokenSprite = GetPrivate<Sprite>(rock, "alreadyBreakRock");
        SpriteRenderer sr = GetPrivate<SpriteRenderer>(rock, "spriteRenderer");
        Animator animator = GetPrivate<Animator>(rock, "animator");

        Assert.IsNotNull(brokenSprite, "prefab ยังไม่ได้ assign 'alreadyBreakRock' (sprite ตอนแตก)");
        Assert.IsNotNull(sr, "BreakableRock หา spriteRenderer ไม่เจอ");
        Assert.IsNotNull(animator, "BreakableRock หา animator ไม่เจอ");

        // ให้ animator เล่นเสมอแม้ไม่มีกล้องเรนเดอร์ใน test scene (กัน Animation Event ไม่ยิงเพราะถูก cull)
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Sprite originalSprite = sr.sprite;
        Debug.Log($"[TEST] ก่อนทุบ: sprite='{SpriteName(originalSprite)}'  broken target='{brokenSprite.name}'");
        Assert.AreNotEqual(brokenSprite, originalSprite,
            "เริ่มต้น sprite ไม่ควรเป็น sprite ตอนแตกอยู่แล้ว ไม่งั้นเช็คการเปลี่ยนไม่ได้");

        // เล่น break animation จริง (controller + clip + ChangeSprite Animation Event ของจริง)
        // เลือก Version2 แบบ deterministic (Version1 ก็มี ChangeSprite event เดียวกัน)
        animator.SetBool("IsVer1", false);
        animator.SetTrigger("Trigger");

        // ── ติดตาม sprite ทุก 0.1s เพื่อดูไทม์ไลน์: ระหว่างเล่นเป็นเฟรม animation, จบแล้วค้างที่อะไร ──
        float elapsed = 0f;
        const float watch = 1.6f; // เกินความยาว clip (~1.02s) เผื่อ transition ไป EmptryState จบ
        while (elapsed < watch)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[TEST] t={elapsed:F1}s sprite='{SpriteName(sr.sprite)}' normTime={st.normalizedTime:F2}");
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        // ── ผลลัพธ์ที่ต้องการ: จบ animation แล้ว sprite ต้องค้างเป็น "หินแตก" ──
        Assert.AreEqual(brokenSprite, sr.sprite,
            $"จบ animation แล้ว sprite ควรเป็น '{brokenSprite.name}' (หินแตก) แต่กลับเป็น '{SpriteName(sr.sprite)}' — " +
            "แปลว่า ChangeSprite event ไม่ยิง หรือ animator ยังขับ sprite ทับ (ไม่ได้ไปจบที่ state ที่ไม่มี motion)");
        Assert.AreNotEqual(originalSprite, sr.sprite,
            "จบ animation แล้ว sprite กลับไปเป็นอันแรก = บั๊กที่รายงาน");
    }

    private static string SpriteName(Sprite s) => s != null ? s.name : "null";

    private static T GetPrivate<T>(object obj, string field) where T : class
    {
        var f = obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"หา field '{field}' ใน {obj.GetType().Name} ไม่เจอ (ชื่อ field อาจเปลี่ยนไป)");
        return f.GetValue(obj) as T;
    }
}
