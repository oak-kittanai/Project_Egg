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
        Debug.Log("Connect Success");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Fusion: Disconnected from server reason + : {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (SessionManager.Instance != null)
        {
            Debug.Log("Session not null");
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

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        bool tutorialBlocking = TutorialUIManager.Instance != null && TutorialUIManager.Instance.IsTutorialOpen;

        if (!tutorialBlocking)
        {
            bool jump = false, press_F = false, press_E = false, press_ESC = false, press_TAB = false, press_Q = false, press_G = false;
            Vector2 mousePosition = Vector2.zero;

            // ---- Keyboard ----
            float kbMoveX = 0f, kbMoveY = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) kbMoveX = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) kbMoveX = 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) kbMoveY = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) kbMoveY = -1f;

                if (Keyboard.current.spaceKey.isPressed || Keyboard.current.upArrowKey.isPressed) jump = true;
                if (Keyboard.current.fKey.isPressed) press_F = true;
                if (Keyboard.current.eKey.isPressed) press_E = true;
                if (Keyboard.current.escapeKey.isPressed) press_ESC = true;
                if (Keyboard.current.tabKey.isPressed) press_TAB = true;
                if (Keyboard.current.qKey.isPressed) press_Q = true;
                if (Keyboard.current.gKey.isPressed) press_G = true;
            }

            // ---- Mouse (อ่านแยกอิสระ ไม่ผูกกับว่ามี Keyboard ไหม) ----
            if (Mouse.current != null) mousePosition = Mouse.current.position.ReadValue();

            // ---- Gamepad — ปุ่ม bool OR กับ Keyboard ตามปกติ, แกนเดิน Keyboard มีสิทธิ์เหนือกว่าถ้าขัดกัน ----
            float gpMoveX = 0f, gpMoveY = 0f;
            if (Gamepad.current != null)
            {
                var gp = Gamepad.current;

                if (gp.leftStick.left.isPressed || gp.dpad.left.isPressed) gpMoveX = -1f;
                if (gp.leftStick.right.isPressed || gp.dpad.right.isPressed) gpMoveX = 1f;
                if (gp.leftStick.up.isPressed || gp.dpad.up.isPressed) gpMoveY = 1f;
                if (gp.leftStick.down.isPressed || gp.dpad.down.isPressed) gpMoveY = -1f;

                if (gp.buttonSouth.isPressed) jump = true;       // A = Jump
                if (gp.rightShoulder.isPressed) { press_F = true; press_Q = true; } // R1 = F (เป็ดดำน้ำ) + Throw (นกปาหิน)
                if (gp.buttonWest.isPressed) press_E = true;     // X = Interact
                if (gp.leftShoulder.isPressed) press_G = true;   // L1 = Drop Item
                if (gp.startButton.isPressed) press_ESC = true;  // Start = ESC (Pause Menu)
                if (gp.buttonEast.isPressed) press_TAB = true;   // B = Tab (ปิด UI)
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

        Debug.Log($"Player has {player.PlayerId} Joined the session");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player has Disconnect");
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
            Debug.Log("Found room: " + sessionList[0].Name);
        }
        else
        {
            Debug.Log("No room found");
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

public struct NetworkInputData : INetworkInput
{
    // Input
    public Vector2 mousePos;
    public float horizontal; // D-Pad / Left Stick
    public float vertical; // D-Pad / Left Stick
    public bool KeybindJump; // A
    public bool Keyboard_F; // R1

    public bool Keyboard_ESC; // Start
    public bool Keyboard_Tab; // B

    public bool KeybindInteract; // X
    public bool KeybindDropItem; // L1
    public bool KeybindThrowItem; // R1
}
