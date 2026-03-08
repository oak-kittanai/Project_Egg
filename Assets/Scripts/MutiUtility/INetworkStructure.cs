using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class INetworkStructure : MonoBehaviour, INetworkRunnerCallbacks
{
    //[SerializeField] public PlayerSpawn spawner;

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
        if (Keyboard.current != null)
        {
            float moveX = 0;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                moveX = -1;
            }
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                moveX = 1;
            }

            float moveY = 0;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                moveY = 1;
            }
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                moveY = -1;
            }

            bool jump = false;
            if (Keyboard.current.spaceKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                jump = true;
            }
            else
            {
                jump = false;
            }

            bool press_F;
            if (Keyboard.current.fKey.isPressed)
            {
                press_F = true;
            }
            else
            {
                press_F = false;
            }

            bool press_E;
            if (Keyboard.current.eKey.isPressed)
            {
                press_E = true;
            }
            else
            {
                press_E = false;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();

            // need to add skill
            /*
            bool skill1 = false;
            if (Keyboard.current.spaceKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                skill = true;
            }
            else
            {
                skill = false;
            }
            data.skill_1 = skill1
            */

            data.mousePos = mousePosition;
            data.horizontal = moveX;
            data.vertical = moveY;
            data.jump = jump;
            data.Keyboard_F = press_F;
            data.Keyboard_E = press_E;
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

        // for test
        //if (SpawnPlayer.Instance != null)
        //{
        //    if (runner.IsServer)
        //    {
        //        SpawnPlayer.Instance.SpawnPlayerToPosition(player, runner);
        //    }
        //    Debug.Log("Try spawn");
        //}
        //else
        //{
        //    Debug.LogWarning("Can't find CenterHost");
        //}

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

    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {

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
    public Vector2 mousePos;
    public float horizontal;
    public float vertical;
    public bool jump;
    public bool Keyboard_F;
    public bool Keyboard_E;
    //public bool skill_1;
    //public bool skill_2;
}
