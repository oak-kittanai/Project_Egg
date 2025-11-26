using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class INetworkStructure : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] public PlayerSpawn spawner;

    #region OnConnected&Disconnected

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connect Success");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

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
        var data = new NetworkdInputData();
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

            bool jump = false;
            if (Keyboard.current.spaceKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                jump = true;
            }
            else
            {
                jump = false;
            }

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

            data.horizontal = moveX;
            data.jump = jump;
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
        if (spawner != null)
        {
            if (runner.IsServer)
            {
                spawner.SpawnPlayer_RPC(player);
                Debug.Log("Spawner is: " + spawner);
            }
        }

        Debug.Log($"Player has {player.PlayerId} Joined");
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

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {

    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {

    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {

    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }

    #endregion
}

public struct NetworkdInputData : INetworkInput
{
    public float horizontal;
    public bool jump;
    //public bool skill_1;
    //public bool skill_2;
}
