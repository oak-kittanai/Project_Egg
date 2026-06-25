using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Fusion;
using Assert = NUnit.Framework.Assert;

public class BirdMovesetIntegrationTests
{
    private const string birdPrefabPath = "Prefabs/Character_Prefabs/Player(Bird)";
    private const string duckPrefabPath = "Prefabs/Character_Prefabs/Player(Duck)";
    private const string gameManagerPrefabPath = "Prefabs/GameManager";

    private NetworkRunner runner;
    private NetworkObject birdNetObj;
    private NetworkObject duckNetObj;
    private NetworkObject gameManagerNetObj;
    private Bird_Moveset birdMoveset;
    private Duck_Moveset duckMoveset;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        // 1) ต้องมี NetworkRunner จริงก่อน ไม่งั้น [Networked] property ทุกตัวจะ throw
        var runnerObj = new GameObject("TestRunner");
        runner = runnerObj.AddComponent<NetworkRunner>();
        runner.ProvideInput = false;
        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();

        var startArgs = new StartGameArgs
        {
            GameMode = GameMode.Single, // เล่นคนเดียวในเครื่อง ไม่ต้องต่อ Photon Cloud จริง
            SessionName = "StunTest_" + System.Guid.NewGuid(),
            SceneManager = sceneManager,
        };

        var startTask = runner.StartGame(startArgs);
        yield return new WaitUntil(() => startTask.IsCompleted);
        Assert.IsTrue(startTask.Result.Ok, $"NetworkRunner start failed: {startTask.Result.ShutdownReason}");

        // 2) เสกนกและเป็ดขึ้นมาจริงผ่าน Runner.Spawn (ไม่ใช่ new GameObject().AddComponent)
        NetworkObject birdPrefab = Resources.Load<NetworkObject>(birdPrefabPath);
        NetworkObject duckPrefab = Resources.Load<NetworkObject>(duckPrefabPath);
        Assert.IsNotNull(birdPrefab, $"ไม่พบ prefab ที่ Resources/{birdPrefabPath}");
        Assert.IsNotNull(duckPrefab, $"ไม่พบ prefab ที่ Resources/{duckPrefabPath}");

        birdNetObj = runner.Spawn(birdPrefab, Vector3.zero, Quaternion.identity);
        duckNetObj = runner.Spawn(duckPrefab, new Vector3(3f, 0f, 0f), Quaternion.identity); // เป็ดอยู่ห่างไปทางขวา 3m

        yield return null; // รอ 1 tick ให้ Spawned() ของทั้งคู่รันจบ

        birdMoveset = birdNetObj.GetComponent<Bird_Moveset>();
        duckMoveset = duckNetObj.GetComponent<Duck_Moveset>();
        Assert.IsNotNull(birdMoveset, "Spawn แล้วไม่มี Bird_Moveset");
        Assert.IsNotNull(duckMoveset, "Spawn แล้วไม่มี Duck_Moveset");
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        if (runner != null && runner.IsRunning)
        {
            if (gameManagerNetObj != null) runner.Despawn(gameManagerNetObj);
            if (birdNetObj != null) runner.Despawn(birdNetObj);
            if (duckNetObj != null) runner.Despawn(duckNetObj);
            yield return null; // ให้ OnDestroy() ของ SingletonNetwork เคลียร์ static instance จริง
        }

        if (runner != null)
        {
            runner.Shutdown();
            yield return new WaitUntil(() => runner.IsShutdown);
            Object.DestroyImmediate(runner.gameObject);
        }

        gameManagerNetObj = null;
    }

    [UnityTest]
    public IEnumerator BirdThrowsRockAtDuck_DuckBecomesStunned()
    {
        // ARRANGE — ปาหินต้องผ่าน GameManager.Instance.ProjectileSpawn() ดังนั้นต้อง
        // เสก GameManager ขึ้นมาในการทดลองนี้ด้วย
        NetworkObject gameManagerPrefab = Resources.Load<NetworkObject>(gameManagerPrefabPath);
        Assert.IsNotNull(gameManagerPrefab, $"ไม่พบ prefab ที่ Resources/{gameManagerPrefabPath}");
        gameManagerNetObj = runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
        yield return null; // รอ GameManager.Spawned() รันจบ

        // ขั้น 3: เล็งไปทิศทางของเป็ด (เป็ดอยู่ทางขวาจาก Setup แล้ว -> หันไปทางขวา)
        birdMoveset.throwPoint.right = Vector3.right;
        birdMoveset._canThrowItem = true; // เติมกระสุนหินให้นก (ปกติได้จากการหยิบหินจริงในเกม)

        Assert.IsFalse(duckMoveset.isStunned, "เริ่ม test มา เป็ดไม่ควรติด Stun อยู่แล้ว");

        // ขั้น 2: รันคำสั่งปาหิน (เรียก method จริงที่เกมใช้ ไม่ลัดผ่าน TriggerStun ตรงๆ)
        birdMoveset.ExecuteThrow();

        yield return null; // 1 tick ให้ Runner.Spawn() ของหินเสร็จสมบูรณ์

        // DIAGNOSTIC 1: หินมีตัวตนอยู่จริงไหม (Collider2D)
        RockObject[] rocks = Object.FindObjectsByType<RockObject>(FindObjectsSortMode.None);
        Assert.AreEqual(1, rocks.Length, $"คาดว่ามีหิน 1 ก้อนหลังปา แต่เจอ {rocks.Length} ก้อน");
        RockObject rock = rocks[0];

        Collider2D rockCollider = rock.GetComponent<Collider2D>();
        Assert.IsNotNull(rockCollider, "หินไม่มี Collider2D เลย!");
        Debug.Log($"[DIAG] Rock spawned at {rock.transform.position}, layer={LayerMask.LayerToName(rock.gameObject.layer)}, colliderEnabled={rockCollider.enabled}");

        // DIAGNOSTIC 2: หินวิ่งไปทางเป็ดจริงไหม (velocity)
        Rigidbody2D rockRb = rock.GetComponent<Rigidbody2D>();
        Debug.Log($"[DIAG] Rock velocity = {rockRb.linearVelocity}");
        Assert.Greater(rockRb.linearVelocity.x, 0f, "หินควรมี velocity ทางขวา (ไปทางเป็ด) แต่ไม่มี");

        // DIAGNOSTIC 3: Layer หากันเจอไหม (Collision Matrix)
        int rockLayer = rock.gameObject.layer;
        int duckLayer = duckMoveset.gameObject.layer;
        bool ignored = Physics2D.GetIgnoreLayerCollision(rockLayer, duckLayer);
        Debug.Log($"[DIAG] RockLayer={LayerMask.LayerToName(rockLayer)}({rockLayer}), DuckLayer={LayerMask.LayerToName(duckLayer)}({duckLayer}), IgnoreCollision={ignored}");
        Assert.IsFalse(ignored, $"Physics2D ตั้งให้ Layer {LayerMask.LayerToName(rockLayer)} กับ {LayerMask.LayerToName(duckLayer)} ไม่ชนกัน! ไปแก้ใน Edit > Project Settings > Physics 2D > Layer Collision Matrix");

        // DIAGNOSTIC 4: เป็ด (collider เดียวกับที่หินจะชน) หา IstunAble เจอไหม
        Collider2D duckCollider = duckMoveset.GetComponent<Collider2D>();
        Assert.IsNotNull(duckCollider, "เป็ดไม่มี Collider2D บน GameObject เดียวกับ Duck_Moveset!");
        bool foundIstunAble = duckCollider.TryGetComponent<IstunAble>(out _);
        Debug.Log($"[DIAG] duckCollider.TryGetComponent<IstunAble>() = {foundIstunAble}");
        Assert.IsTrue(foundIstunAble, "Collider ของเป็ดหา IstunAble ไม่เจอ! (Duck_Moveset อาจอยู่คนละ GameObject กับ Collider หลัก)");

        // DIAGNOSTIC 5: ติดตามระยะห่าง real-time ระหว่างรอ ว่าหินเข้าใกล้/ชน/หายไปไหม
        float elapsed = 0f;
        while (elapsed < 2f && !duckMoveset.isStunned)
        {
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;

            RockObject[] stillThere = Object.FindObjectsByType<RockObject>(FindObjectsSortMode.None);
            if (stillThere.Length > 0)
            {
                float dist = Vector2.Distance(stillThere[0].transform.position, duckMoveset.transform.position);
                Debug.Log($"[DIAG] t={elapsed:F1}s rockPos={stillThere[0].transform.position} distToDuck={dist:F2}");
            }
            else
            {
                Debug.Log($"[DIAG] t={elapsed:F1}s หินหายไปแล้ว (despawn ไปแล้ว — เช็คว่าโดนอะไรไปก่อนถึงเป็ด)");
            }
        }

        // ขั้น 4: เป็ดต้องติดตั้นจากการถูกหินชน (ผ่าน RockObject.OnCollisionEnter2D -> IstunAble.TriggerStun() -> ApplyStun())
        Assert.IsTrue(duckMoveset.isStunned, "ปาหินจริงแล้ว แต่เป็ดไม่ติด Stun!");
        Assert.IsFalse(birdMoveset._canThrowItem, "ปาไปแล้ว กระสุนหินต้องหมด");
    }

    [UnityTest]
    public IEnumerator ShowStunIcon_OnLocalPlayer_ActivatesIconAndPlaysAnimation()
    {
        // ทดสอบ PlayerGUI ที่ wire ไว้จริงบน prefab Player(Duck) (StunAni: Image + Animator)
        PlayerGUI gui = duckMoveset.localGUI;
        Assert.IsNotNull(gui, "Duck ต้องมี PlayerGUI (localGUI) ติดอยู่");
        Assert.IsNotNull(gui.stunIcon, "ต้อง wire stunIcon (StunAni) ใน Inspector ของ Player(Duck) ก่อน");
        Assert.IsNotNull(gui.stunAnimator, "ต้อง wire stunAnimator ใน Inspector ของ Player(Duck) ก่อน");

        gui.HideStunIcon();
        yield return null;
        Assert.IsFalse(gui.stunIcon.activeSelf, "ก่อนเรียก ShowStunIcon ต้องยังไม่โชว์");

        gui.ShowStunIcon();
        yield return null;

        Assert.IsTrue(gui.stunIcon.activeSelf, "ShowStunIcon ต้อง SetActive(true) ให้ stunIcon");

        yield return new WaitForSeconds(0.1f); // ให้ Animator เข้า state สัก frame

        var stateInfo = gui.stunAnimator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"[DIAG] Stun Animator state length={stateInfo.length}, normalizedTime={stateInfo.normalizedTime}");
        Assert.Greater(stateInfo.length, 0f,
            "Animator ต้องมี state กำลังเล่นอยู่จริงหลังเรียก Play(\"Stun\") — ถ้า length=0 แปลว่า Controller ไม่มี state ชื่อ \"Stun\"");

        gui.HideStunIcon();
    }

    [UnityTest]
    public IEnumerator ShowMiniDialogue_OnLocalPlayer_ActivatesContainer()
    {
        // เส้นทางเดียวกับที่ TriggerDialogue (isSubDialogue=true) เรียกผ่าน ShowSubDialogueOnLocalPlayer
        PlayerGUI gui = duckMoveset.localGUI;
        Assert.IsNotNull(gui.miniDialogueContainer, "ต้อง wire miniDialogueContainer (MiniDialogue) ใน Inspector ของ Player(Duck) ก่อน");

        var lineJson = new TextAsset("{\"lines\":[{\"speaker\":\"Duck\",\"thai\":\"ทดสอบ\",\"eng\":\"Test line\"}]}");
        var config = new DialogueConfig { isThaiLanguage = false, effect = TextEffectType.None, JsonFile = lineJson };

        gui.HideMiniDialogue();
        yield return null;
        Assert.IsFalse(gui.miniDialogueContainer.activeSelf, "ก่อนเรียก ShowMiniDialogue ต้องยังไม่โชว์");

        gui.ShowMiniDialogue(new DialogueConfig[] { config });
        yield return null;

        Assert.IsTrue(gui.miniDialogueContainer.activeSelf,
            "ShowMiniDialogue ต้องเปิด miniDialogueContainer — เส้นทางเดียวกับที่ TriggerDialogue.RPC_TriggerSubDialogueNetwork เรียกผ่าน ShowSubDialogueOnLocalPlayer");

        gui.HideMiniDialogue();
    }

    [UnityTest]
    public IEnumerator StartDialogueSequence_ActivatesMainDialoguePanel()
    {
        // เส้นทางเดียวกับที่ TriggerDialogue (isSubDialogue=false) เรียกผ่าน RPC_TriggerDialogueNetwork
        // หมายเหตุ: DialogueHUB/DialogueManager.Awake() เซ็ต Instance = this แบบไม่มีเงื่อนไขอยู่แล้ว
        // (ไม่เหมือน TutorialUIManager ที่กันซ้ำ) จึงไม่ต้องเคลียร์ Instance เองก่อนสร้างใหม่

        var canvasGO = new GameObject("Canvas", typeof(RectTransform));
        var dialogueGO = new GameObject("Dialogue", typeof(RectTransform));
        dialogueGO.transform.SetParent(canvasGO.transform);

        var miraBoxGO = new GameObject("MiraBox", typeof(RectTransform));
        miraBoxGO.transform.SetParent(dialogueGO.transform);

        var nextGO = new GameObject("NextButton", typeof(RectTransform));
        nextGO.transform.SetParent(dialogueGO.transform);
        nextGO.AddComponent<Button>();

        var prevGO = new GameObject("PrevButton", typeof(RectTransform));
        prevGO.transform.SetParent(dialogueGO.transform);
        prevGO.AddComponent<Button>();

        dialogueGO.SetActive(false);

        var hubGO = new GameObject("DialogueHUB");
        hubGO.AddComponent<DialogueHUB>();
        DialogueHUB.Instance.FindUIReferences();

        var managerGO = new GameObject("DialogueManager");
        var manager = managerGO.AddComponent<DialogueManager>();

        var lineJson = new TextAsset("{\"lines\":[{\"speaker\":\"\",\"thai\":\"ทดสอบ\",\"eng\":\"Test line\"}]}");
        var config = new DialogueConfig { NameofSpeaker = "Mira", isThaiLanguage = false, effect = TextEffectType.None, JsonFile = lineJson };

        manager.StartDialogueSequence(new DialogueConfig[] { config });
        yield return null;

        Assert.IsTrue(dialogueGO.activeSelf,
            "StartDialogueSequence ต้องเปิด Dialogue panel ผ่าน DialogueHUB.DisplayLine — เส้นทางเดียวกับที่ TriggerDialogue.RPC_TriggerDialogueNetwork เรียก");
        Assert.IsTrue(miraBoxGO.activeSelf, "Speaker เป็น \"Mira\" ต้องเปิด MiraBox ด้วย");

        Object.Destroy(canvasGO);
        Object.Destroy(hubGO);
        Object.Destroy(managerGO);
    }
}
