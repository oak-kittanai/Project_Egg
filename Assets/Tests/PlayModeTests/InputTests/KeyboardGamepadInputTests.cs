using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Fusion;
using Assert = NUnit.Framework.Assert;

public class KeyboardGamepadInputTests
{
    private const string birdPrefabPath = "Prefabs/Character_Prefabs/Player(Bird)";
    private const string gameManagerPrefabPath = "Prefabs/GameManager";

    private NetworkRunner runner;
    private NetworkObject birdNetObj;
    private NetworkObject gameManagerNetObj;
    private Bird_Moveset birdMoveset;
    private Rigidbody2D birdRb;
    private INetworkStructure netStructure;
    private InputSettings.UpdateMode originalUpdateMode;
    private GameObject groundGO;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        originalUpdateMode = InputSystem.settings.updateMode;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        ReleaseAllKeys();

        var runnerObj = new GameObject("TestRunner");
        runner = runnerObj.AddComponent<NetworkRunner>();
        runner.ProvideInput = true; // ต้องเป็น true เพื่อให้ Fusion เรียก OnInput() จริงทุก tick
        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();
        netStructure = runnerObj.AddComponent<INetworkStructure>();
        runner.AddCallbacks(netStructure);

        var startArgs = new StartGameArgs
        {
            GameMode = GameMode.Single,
            SessionName = "InputTest_" + System.Guid.NewGuid(),
            SceneManager = sceneManager,
        };

        var startTask = runner.StartGame(startArgs);
        yield return new WaitUntil(() => startTask.IsCompleted);
        Assert.IsTrue(startTask.Result.Ok, $"NetworkRunner start failed: {startTask.Result.ShutdownReason}");

        NetworkObject birdPrefab = Resources.Load<NetworkObject>(birdPrefabPath);
        Assert.IsNotNull(birdPrefab, $"ไม่พบ prefab ที่ Resources/{birdPrefabPath}");

        birdNetObj = runner.Spawn(birdPrefab, Vector3.zero, Quaternion.identity, inputAuthority: runner.LocalPlayer);
        yield return null;

        birdMoveset = birdNetObj.GetComponent<Bird_Moveset>();
        Assert.IsNotNull(birdMoveset, "Spawn แล้วไม่มี Bird_Moveset");
        birdRb = birdMoveset.GetComponent<Rigidbody2D>();
        Assert.IsNotNull(birdRb, "Bird ไม่มี Rigidbody2D");

        // MovementCharacter.FixedUpdateNetwork() return ตั้งแต่ต้นถ้าไม่มี GameManager หรือ
        // ยังไม่ isLoadMapDone/IsGameReady — เลยต้อง spawn + บังคับ ready เอง ข้าม flow โหลดแมพจริง
        NetworkObject gameManagerPrefab = Resources.Load<NetworkObject>(gameManagerPrefabPath);
        Assert.IsNotNull(gameManagerPrefab, $"ไม่พบ prefab ที่ Resources/{gameManagerPrefabPath}");
        gameManagerNetObj = runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
        yield return null;

        GameManager.Instance.isLoadMapDone = true;
        GameManager.Instance.IsGameReady = true;
        yield return null;

        // วางพื้นให้ IsGrounded เป็นจริง (CheckGround ใช้ Physics2D.Raycast ลงล่างเช็ค layer Ground/Platform)
        // ใช้ร่วมกันทุกเทสต์ในคลาสนี้ ไม่ใช่แค่เทสต์ Jump
        groundGO = new GameObject("TestGround", typeof(BoxCollider2D));
        groundGO.layer = LayerMask.NameToLayer("Ground");
        groundGO.transform.position = new Vector3(0f, -1.5f, 0f);
        groundGO.GetComponent<BoxCollider2D>().size = new Vector2(20f, 2f);
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        ReleaseAllKeys();
        ReleaseAllGamepadDevices();
        InputSystem.settings.updateMode = originalUpdateMode;

        if (groundGO != null) Object.Destroy(groundGO);

        if (runner != null && runner.IsRunning)
        {
            if (gameManagerNetObj != null) runner.Despawn(gameManagerNetObj);
            if (birdNetObj != null) runner.Despawn(birdNetObj);
            yield return null;
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
    public IEnumerator Keyboard_MovesCharacter_WhenNoGamepadPresent()
    {
        Assert.IsNull(Gamepad.current, "เทสต์นี้ต้องไม่มี gamepad ต่ออยู่เลย");

        PressKey(Key.D);
        yield return WaitForTicks(5);

        Debug.Log($"[DIAG] velocity={birdRb.linearVelocity}");
        Assert.Greater(birdRb.linearVelocity.x, 0f, "กด D (keyboard อย่างเดียว ไม่มี gamepad) ต้องเดินขวา");

        ReleaseAllKeys();
    }

    [UnityTest]
    public IEnumerator Gamepad_MovesCharacter_WhenNoKeyboardPressed()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        yield return null;

        SetGamepadStick(gamepad, new Vector2(1f, 0f));
        yield return WaitForTicks(5);

        Debug.Log($"[DIAG] velocity={birdRb.linearVelocity}");
        Assert.Greater(birdRb.linearVelocity.x, 0f, "ดัน stick ขวา (ไม่กด keyboard เลย) ต้องเดินขวา");

        ResetGamepad(gamepad);
        InputSystem.RemoveDevice(gamepad);
    }

    [UnityTest]
    public IEnumerator Keyboard_WinsOverGamepad_OnConflictingDirection()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        yield return null;

        PressKey(Key.D);            // keyboard: ขวา
        SetGamepadStick(gamepad, new Vector2(-1f, 0f)); // gamepad: ซ้าย (ขัดกัน)
        yield return WaitForTicks(5);

        Debug.Log($"[DIAG] velocity={birdRb.linearVelocity} (คาดว่า > 0 เพราะ keyboard ต้องชนะ)");
        Assert.Greater(birdRb.linearVelocity.x, 0f,
            "Keyboard (ขวา) กับ Gamepad (ซ้าย) ขัดกัน — Keyboard ต้องชนะเสมอตามที่ตกลงกัน");

        ReleaseAllKeys();
        ResetGamepad(gamepad);
        InputSystem.RemoveDevice(gamepad);
    }

    [UnityTest]
    public IEnumerator RemovingGamepadEntirely_KeyboardStillWorks()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        yield return null;
        InputSystem.RemoveDevice(gamepad);
        yield return null;

        Assert.IsNull(Gamepad.current, "ถอด gamepad ออกแล้วต้องไม่มี Gamepad.current เหลือ");

        PressKey(Key.A);
        yield return WaitForTicks(5);

        Debug.Log($"[DIAG] velocity={birdRb.linearVelocity}");
        Assert.Less(birdRb.linearVelocity.x, 0f, "ถอด Controller ออกหมดแล้ว Keyboard (A) ต้องยังเดินซ้ายได้ปกติ");

        ReleaseAllKeys();
    }

    [UnityTest]
    public IEnumerator Jump_WorksFromBothKeyboardAndGamepad()
    {
        // พื้นถูกสร้างไว้แล้วใน [UnitySetUp] (groundGO) ใช้ร่วมกันทุกเทสต์
        // ปล่อยให้ bird ตกลงไปยืนบนพื้นจริงๆก่อน (ไม่ลัดผ่าน เพื่อให้ IsGrounded ได้ค่าจาก physics จริง)
        yield return WaitForTicks(60);
        Debug.Log($"[DIAG] หลังตกพื้น: pos={birdMoveset.transform.position}, IsGrounded={birdMoveset.IsGrounded}, velocity={birdRb.linearVelocity}");
        Assert.IsTrue(birdMoveset.IsGrounded, "Bird ควรตกลงไปยืนบนพื้นทดสอบและ IsGrounded=true แล้วก่อนเริ่มทดสอบกระโดด");

        // ทุก Bird ที่สปอนใหม่ติด JumpCooldown 2 วินาที (กันกระโดดสปอนแรก ดู MovementCharacter.Spawned())
        // ต้องรอเวลาจริงให้พ้นคูลดาวน์ก่อน ไม่ใช่แค่รอ tick เฉยๆ
        yield return new WaitForSeconds(2.2f);

        // 1) Jump ผ่าน Keyboard (Space)
        // เช็คทีละ tick ทันที (poll) ไม่ใช่รอคงที่แล้วเช็คทีเดียว — เพราะถ้าปุ่มยังกดอยู่แต่ดันไปเช็ค
        // ตอนอาร์คกระโดดร่วงลงมาแล้ว (ApplyJumpCut/gravity ปกติพาลงไปแล้ว) จะเจอ velocity.y ติดลบทั้งที่กระโดดสำเร็จจริง
        PressKey(Key.Space);
        bool sawPositiveY = false;
        float pollDeadline = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < pollDeadline && !sawPositiveY)
        {
            yield return null;
            if (birdRb.linearVelocity.y > 0f) sawPositiveY = true;
        }
        Debug.Log($"[DIAG] หลังกด Space: velocity={birdRb.linearVelocity}, sawPositiveY={sawPositiveY}");
        Assert.IsTrue(sawPositiveY, "กด Space (keyboard) ตอนยืนอยู่บนพื้น ต้องกระโดด (เคยเห็น velocity.y > 0 ระหว่างถือปุ่มอยู่)");
        ReleaseAllKeys();

        // กระโดดสำเร็จแล้ว HandleJump() reset JumpCooldown ใหม่อีก 2 วินาที (ดู MovementCharacter.cs:580)
        // ต้องรอเวลาจริงให้พ้นคูลดาวน์ก่อนลองฝั่ง gamepad ไม่ใช่แค่รอ tick เฉยๆ
        yield return new WaitForSeconds(2.2f); // รอให้ตกกลับลงพื้นและพ้นคูลดาวน์ก่อนลองฝั่ง gamepad

        // 2) Jump ผ่าน Gamepad (South / A)
        var gamepad = InputSystem.AddDevice<Gamepad>();
        yield return null;
        PressGamepadButton(gamepad, GamepadButton.South);
        sawPositiveY = false;
        pollDeadline = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < pollDeadline && !sawPositiveY)
        {
            yield return null;
            if (birdRb.linearVelocity.y > 0f) sawPositiveY = true;
        }
        Debug.Log($"[DIAG] หลังกด Gamepad South: velocity={birdRb.linearVelocity}, sawPositiveY={sawPositiveY}");
        Assert.IsTrue(sawPositiveY, "กด South button (gamepad) ตอนยืนอยู่บนพื้น ต้องกระโดด (เคยเห็น velocity.y > 0 ระหว่างถือปุ่มอยู่)");

        ResetGamepad(gamepad);
        InputSystem.RemoveDevice(gamepad);
    }

    [UnityTest]
    public IEnumerator Escape_OpensPause_FromBothKeyboardAndGamepad()
    {
        // GameManager spawn ไว้แล้วใน [UnitySetUp] (gameManagerNetObj)
        // ต้องมี MenuController จริงให้ GameManager.RequestOpenMenu() เรียกใช้ ไม่งั้น IsPaused จะไม่ขยับ
        NetworkObject menuControllerPrefab = Resources.Load<NetworkObject>("NonScriptPrefabs/Network_MenuController");
        Assert.IsNotNull(menuControllerPrefab, "ไม่พบ prefab ที่ Resources/NonScriptPrefabs/Network_MenuController");
        var menuControllerNetObj = runner.Spawn(menuControllerPrefab, Vector3.zero, Quaternion.identity);
        yield return null;

        Assert.IsFalse(GameManager.Instance.IsPaused, "เริ่ม test มา ต้องยังไม่ Pause");

        // 1) ESC ผ่าน Keyboard
        PressKey(Key.Escape);
        yield return WaitForTicks(3);
        Debug.Log($"[DIAG] IsPaused หลังกด Esc (keyboard) = {GameManager.Instance.IsPaused}");
        Assert.IsTrue(GameManager.Instance.IsPaused, "กด Esc (keyboard) ต้องเปิด Pause Menu ผ่าน GameManager.IsPaused");
        ReleaseAllKeys();

        // reset กลับมา unpause เพื่อทดสอบฝั่ง gamepad ต่อ
        // ต้อง reset MenuController.IsMenuOpen ด้วย ไม่ใช่แค่ GameManager.IsPaused เพราะ
        // RequestOpenMenu() เช็ค "if (MenuController.Instance.IsMenuOpen) return;" ก่อนจะเปิด pause รอบใหม่
        GameManager.Instance.SetPause_RPC(false);
        MenuController.Instance.IsMenuOpen = false;
        yield return WaitForTicks(3);
        Assert.IsFalse(GameManager.Instance.IsPaused, "ต้อง unpause กลับมาได้ก่อนเทสต์ฝั่ง gamepad");

        // 2) ESC ผ่าน Gamepad (Start)
        var gamepad = InputSystem.AddDevice<Gamepad>();
        yield return null;
        PressGamepadButton(gamepad, GamepadButton.Start);
        yield return WaitForTicks(3);
        Debug.Log($"[DIAG] IsPaused หลังกด Start (gamepad) = {GameManager.Instance.IsPaused}");
        Assert.IsTrue(GameManager.Instance.IsPaused, "กด Start (gamepad) ต้องเปิด Pause Menu ผ่าน GameManager.IsPaused เหมือนกัน");

        ResetGamepad(gamepad);
        InputSystem.RemoveDevice(gamepad);
        runner.Despawn(menuControllerNetObj);
    }

    private static IEnumerator WaitForTicks(int n)
    {
        for (int i = 0; i < n; i++) yield return null;
    }

    private static void PressKey(Key key)
    {
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
        InputSystem.Update();
    }

    private static void ReleaseAllKeys()
    {
        if (Keyboard.current == null) return;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        InputSystem.Update();
    }

    private static void SetGamepadStick(Gamepad gamepad, Vector2 stick)
    {
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = stick });
        InputSystem.Update();
    }

    private static void PressGamepadButton(Gamepad gamepad, GamepadButton button)
    {
        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(button));
        InputSystem.Update();
    }

    private static void ResetGamepad(Gamepad gamepad)
    {
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();
    }

    private static void ReleaseAllGamepadDevices()
    {
        while (Gamepad.current != null)
        {
            InputSystem.RemoveDevice(Gamepad.current);
        }
    }
}
