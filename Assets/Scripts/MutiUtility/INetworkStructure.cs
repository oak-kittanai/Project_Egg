using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class INetworkStructure : MonoBehaviour, INetworkRunnerCallbacks
{
    #region OnConnected&Disconnected

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Fusion: Disconnected from server reason + : {reason}");
        VerifyLog.Log($"OnDisconnectedFromServer reason={reason} SessionManager.Instance null={SessionManager.Instance == null} runnerRunning={runner != null && runner.IsRunning} -> willShutdown={GameManager.IsInGameplay && runner != null && runner.IsRunning}"); // TEMP verify C-1

        // อยู่ในเกมแล้ว host หลุด -> ปิด runner ตัวเอง (จะวิ่งเข้า OnShutdown -> ReturnToSession)
        if (GameManager.IsInGameplay && runner != null && runner.IsRunning)
            runner.Shutdown();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        VerifyLog.Log($"OnShutdown reason={shutdownReason} inGameplay={GameManager.IsInGameplay} -> branch={(GameManager.IsInGameplay ? "IN-GAME (ReturnToSession)" : "LOBBY (DisconnedFromServer)")}"); // TEMP verify C-1
        Debug.Log($"Fusion: Shutdown reason: {shutdownReason} | inGameplay={GameManager.IsInGameplay}");

        if (GameManager.IsInGameplay)
        {
            // อยู่ในเกม (runner ปิด ไม่ว่าจะกดออกเอง เข้าประตูสุดท้าย หรืออีกฝั่งหลุด) -> กลับหน้า session
            GameManager.ReturnToSession();
        }
        else if (SessionManager.Instance != null)
        {
            // อยู่หน้า menu/lobby -> รีเซ็ตสถานะ lobby ตามเดิม
            SessionManager.Instance.DisconnedFromServer();
        }
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {

    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {

    }

    #endregion

    #region Input

    private static bool _verifyWasBlocked; // TEMP verify H-2

    private bool _lastGamepadButtons;

    // TEMP verify IN-2: ใช้คีย์บอร์ดอยู่แต่จอยยังรายงานว่ากดปุ่มค้าง — ยืนยันว่าปุ่มค้างจริงบนเครื่องผู้เล่น (log เฉพาะตอนค่าเปลี่ยน)
    private static string _verifyStuckPad = "";

    private static void VerifyLogStuckPadButtons(Gamepad gp)
    {
        string stuck = (gp.buttonSouth.isPressed ? "A " : "") + (gp.buttonWest.isPressed ? "X " : "")
                     + (gp.rightShoulder.isPressed ? "RB " : "") + (gp.leftShoulder.isPressed ? "LB " : "")
                     + (gp.startButton.isPressed ? "Start " : "") + (gp.buttonEast.isPressed ? "B " : "");
        if (stuck == _verifyStuckPad) return;

        _verifyStuckPad = stuck;
        VerifyLog.Log($"IN-2 pad '{gp.name}' ({gp.GetType().Name}) buttons held while using keyboard: [{stuck}]");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        bool tutorialBlocking = TutorialUIManager.Instance != null && TutorialUIManager.Instance.IsTutorialOpen;
        bool dialogueBlocking = DialogueManager.IsDialogueOpen;

        // TEMP verify H-2: report when gameplay input starts/stops being dropped
        // (เดิม log ทุกวินาทีระหว่าง dialogue — Console ของ Editor บวมตอนเล่นนาน, PF-0)
        bool blocked = tutorialBlocking || dialogueBlocking;
        if (blocked != _verifyWasBlocked)
        {
            _verifyWasBlocked = blocked;
            VerifyLog.Log($"OnInput {(blocked ? "BLOCKED" : "UNBLOCKED")} dialogueBlocking={dialogueBlocking} tutorialBlocking={tutorialBlocking}");
        }

        if (!tutorialBlocking && !dialogueBlocking)
        {
            bool jump = false, press_F = false, press_E = false, press_ESC = false, press_TAB = false, press_Q = false, press_G = false;
            bool press_Switch = false, press_Lift = false; // ใช้เฉพาะโหมด Solo
            Vector2 mousePosition = Vector2.zero;

            // ปุ่มอ่านจากอุปกรณ์ที่ใช้ล่าสุดเท่านั้น (IN-2) — กระโดด/สกิลดูจังหวะกด (WasPressed)
            // ถ้า OR สองอุปกรณ์แล้วปุ่มของจอยค้างอยู่ กดคีย์บอร์ดจะไม่เกิดจังหวะกด ทั้งที่เดินได้ปกติ
            bool gamepadButtons = InputDeviceWatcher.UsingGamepad;

            // tick ที่เพิ่งสลับอุปกรณ์ไม่อ่านปุ่ม 1 tick — บิตเดิมหลุดก่อน การกดครั้งแรกของอุปกรณ์ใหม่จึงนับเป็นจังหวะกด
            bool readButtons = gamepadButtons == _lastGamepadButtons;
            _lastGamepadButtons = gamepadButtons;

            // ---- Keyboard ----
            float kbMoveX = 0f, kbMoveY = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) kbMoveX = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) kbMoveX = 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) kbMoveY = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) kbMoveY = -1f;

                if (!gamepadButtons && readButtons)
                {
                    if (Keyboard.current.spaceKey.isPressed || Keyboard.current.upArrowKey.isPressed) jump = true;
                    if (Keyboard.current.fKey.isPressed) press_F = true;
                    if (Keyboard.current.eKey.isPressed) press_E = true;
                    if (Keyboard.current.escapeKey.isPressed) press_ESC = true;
                    if (Keyboard.current.tabKey.isPressed) press_TAB = true;
                    if (Keyboard.current.qKey.isPressed) press_Q = true;
                    if (Keyboard.current.gKey.isPressed) press_G = true;
                    if (Keyboard.current.rKey.isPressed) press_Switch = true;
                    if (Keyboard.current.yKey.isPressed) press_Lift = true;
                }
            }

            // ---- Mouse (อ่านแยกอิสระ ไม่ผูกกับว่ามี Keyboard ไหม) ----
            if (Mouse.current != null) mousePosition = Mouse.current.position.ReadValue();

            // ---- Gamepad — แกนเดินรวมกับ Keyboard (Keyboard มีสิทธิ์เหนือกว่าถ้าขัดกัน), ปุ่มอ่านเฉพาะตอนใช้จอยอยู่ ----
            float gpMoveX = 0f, gpMoveY = 0f;
            if (Gamepad.current != null)
            {
                var gp = Gamepad.current;

                if (gp.leftStick.left.isPressed || gp.dpad.left.isPressed) gpMoveX = -1f;
                if (gp.leftStick.right.isPressed || gp.dpad.right.isPressed) gpMoveX = 1f;
                if (gp.leftStick.up.isPressed || gp.dpad.up.isPressed) gpMoveY = 1f;
                if (gp.leftStick.down.isPressed || gp.dpad.down.isPressed) gpMoveY = -1f;

                if (gamepadButtons && readButtons)
                {
                    if (gp.buttonSouth.isPressed) jump = true;       // A = Jump
                    if (gp.rightShoulder.isPressed) { press_F = true; press_Q = true; } // R1 = F (เป็ดดำน้ำ) + Throw (นกปาหิน)
                    if (gp.buttonWest.isPressed) press_E = true;     // X = Interact
                    if (gp.leftShoulder.isPressed) press_G = true;   // L1 = Drop Item
                    if (gp.startButton.isPressed) press_ESC = true;  // Start = ESC (Pause Menu)
                    if (gp.buttonEast.isPressed) press_TAB = true;   // B = Tab (ปิด UI)
                    if (gp.rightTrigger.isPressed) press_Switch = true; // RT / R2 = สลับตัว
                    if (gp.buttonNorth.isPressed) press_Lift = true;    // Y / △ = เป็ดยกนก
                }
                else if (!gamepadButtons)
                {
                    VerifyLogStuckPadButtons(gp); // TEMP verify IN-2
                }
            }

            // Keyboard ชนะถ้ามีค่า (ไม่ใช่ 0) — ถ้า Keyboard ไม่ได้กดแกนนี้เลย ค่อย fallback ไป Gamepad
            float moveX = kbMoveX != 0f ? kbMoveX : gpMoveX;
            float moveY = kbMoveY != 0f ? kbMoveY : gpMoveY;

            data.mousePos = mousePosition;
            data.horizontal = moveX;
            data.vertical = moveY;
            data.KeybindJump = jump;
            data.Keyboard_F = press_F;
            data.KeybindInteract = press_E;
            data.Keyboard_ESC = press_ESC;
            data.Keyboard_Tab = press_TAB;
            data.KeybindThrowItem = press_Q;
            data.KeybindDropItem = press_G;
            data.KeybindSwitch = press_Switch;
            data.KeybindLift = press_Lift;
        }

        // ผลการเคลื่อนที่ของตัวเรา (NR-12): ส่งทุก tick แม้ปุ่มถูกปิดระหว่าง dialogue/สอนเล่น
        var local = MovementCharacter.Local;
        if (local != null && local.Object != null && local.Object.IsValid && local.rb2D != null)
        {
            data.OwnerPosition = local.rb2D.position;
            data.OwnerVelocity = local.rb2D.linearVelocity;
            data.OwnerFlags = (uint)local.OwnerFlags | (uint)MovementCharacter.MoveFlags.ReportValid;
            data.OwnerAckSeq = local.AppliedEventSeq;
        }

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {

    }

    #endregion

    #region OnPlayerJoin&Left

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GetData(player.PlayerId, player, runner);
        }
        if (SessionHub.Instance != null)
        {
            SessionHub.Instance.UpdateTMPText(player.PlayerId);
            SessionHub.Instance.SetDefault(runner);
            if (runner.IsServer)
            {
                SessionHub.Instance.SetupButtonOnline(true);
            }
            else
            {
                SessionHub.Instance.SetupButtonOnline(false);
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        VerifyLog.Log($"OnPlayerLeft player={player} inGameplay={GameManager.IsInGameplay} runnerRunning={runner != null && runner.IsRunning} -> willShutdown={GameManager.IsInGameplay && runner != null && runner.IsRunning}"); // TEMP verify C-1

        // co-op 2 คน: ถ้าอยู่ในเกมแล้วอีกคนออก -> ออกทั้งคู่ (ปิด runner -> OnShutdown -> ReturnToSession)
        if (GameManager.IsInGameplay && runner != null && runner.IsRunning)
            runner.Shutdown();
    }

        #endregion

        #region SceneLoadDone&Start

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        GameBootstrapper bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        if (bootstrapper != null)
            bootstrapper.Bootstrap(runner);
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        // Fusion โหลด scene เฉพาะด่าน (lobby โหลดด้วย SceneManager ใน GameManager.ReturnToSession) -> นับว่าเข้าเกมแล้ว
        GameManager.MarkInGameplay();

        if (GlobalLoadingManager.Instance != null)
            GlobalLoadingManager.Instance.ShowLoading();
    }

    #endregion

    #region SessionLoad

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (sessionList.Count > 0)
        {
            SessionManager.Instance.UpdatePlayerCount(runner);
            SessionHub.Instance.UpdateList(sessionList.Count);
        }
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {

    }

    #endregion

    #region Not use

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {

    }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {

    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }

    #endregion
}

// ปุ่มทั้งหมดเก็บเป็น bit ใน NetworkButtons — ใช้ WasPressed(PreviousButtons, ...) จับจังหวะกด
// (MovementCharacter.PreviousButtons เป็น [Networked] จึงถูกย้อนตอน resimulate ไม่ทำงานซ้ำ/หายเหมือน bool ธรรมดา)
public enum InputButton
{
    Jump = 0,     // Space / A
    F = 1,        // R1 (เป็ดดำน้ำ)
    Esc = 2,      // Start
    Tab = 3,      // B
    Interact = 4, // E / X
    Drop = 5,     // G / L1
    Throw = 6,    // Q / R1
    Switch = 7,   // R / RT / R2 — Solo: สลับตัว, นกพาบิน
    Lift = 8,     // Y / Y / △ — Solo: เป็ดยก/วางนก
}

public struct NetworkInputData : INetworkInput
{
    // Input
    public Vector2 mousePos;
    public float horizontal; // D-Pad / Left Stick
    public float vertical; // D-Pad / Left Stick
    public NetworkButtons Buttons;

    // ตัวละครของเครื่องนี้ (NR-12): เจ้าของคำนวณการเคลื่อนที่เองแล้วส่งผลให้ host
    public Vector2 OwnerPosition;
    public Vector2 OwnerVelocity;
    public uint OwnerFlags;
    public byte OwnerAckSeq; // event จาก host ล่าสุดที่ทำแล้ว (วาร์ป/แรงกระแทก)

    // ชื่อเดิม อ่าน/เขียน bit ใน Buttons — โค้ดที่เช็คปุ่มกดค้างใช้ต่อได้เหมือนเดิม
    public bool KeybindJump { get => Buttons.IsSet(InputButton.Jump); set => Buttons.Set(InputButton.Jump, value); }
    public bool Keyboard_F { get => Buttons.IsSet(InputButton.F); set => Buttons.Set(InputButton.F, value); }
    public bool Keyboard_ESC { get => Buttons.IsSet(InputButton.Esc); set => Buttons.Set(InputButton.Esc, value); }
    public bool Keyboard_Tab { get => Buttons.IsSet(InputButton.Tab); set => Buttons.Set(InputButton.Tab, value); }
    public bool KeybindInteract { get => Buttons.IsSet(InputButton.Interact); set => Buttons.Set(InputButton.Interact, value); }
    public bool KeybindDropItem { get => Buttons.IsSet(InputButton.Drop); set => Buttons.Set(InputButton.Drop, value); }
    public bool KeybindThrowItem { get => Buttons.IsSet(InputButton.Throw); set => Buttons.Set(InputButton.Throw, value); }
    public bool KeybindSwitch { get => Buttons.IsSet(InputButton.Switch); set => Buttons.Set(InputButton.Switch, value); }
    public bool KeybindLift { get => Buttons.IsSet(InputButton.Lift); set => Buttons.Set(InputButton.Lift, value); }
}
