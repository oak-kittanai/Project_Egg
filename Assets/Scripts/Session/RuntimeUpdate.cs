using Fusion;
using UnityEngine;

public class RuntimeUpdate : SingletonNetwork<RuntimeUpdate>
{
    public NetworkRunner runner;
    [Networked] bool _isBirdTaken { get; set; }
    [Networked] bool _isDuckTaken { get; set; }

    [Networked] bool ChangeCharacter { get; set; }

    [Networked] public characterType currentHost { get; set; }
    [Networked] public characterType currentClient { get; set; }

    [Networked] int _currentHostType { get; set; }
    [Networked] int _currentClientType { get; set; }

    [Networked] string ServerCode { get; set; }

    [Networked] public NetworkBool isHostReady { get; set; }
    [Networked] public NetworkBool isClientReady { get; set; }

    [Networked] public bool startAble { get; set; }

    public override void Spawned()
    {
        CodeAnnouncement_RPC();
    }

    public override void Render()
    {
        SessionHub.Instance.UpdateOverTime();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void PlayerReady_RPC(bool isHost)
    {
        if (isHost)
        {
            if (!isHostReady && (_currentHostType == 0 || _currentHostType == _currentClientType)) return;

            isHostReady = !isHostReady;
        }
        else
        {
            if (!isClientReady && (_currentClientType == 0 || _currentClientType == _currentHostType)) return;

            isClientReady = !isClientReady;
        }

        if (isHost) UpdatePlayerReady_RPC(isHost, isHostReady);
        else UpdatePlayerReady_RPC(isHost, isClientReady);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void UpdatePlayerReady_RPC(bool isHost, bool isReady)
    {
        SessionHub.Instance.ChangeReadySprite(isHost, isReady);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void ChangeTypeRequest_RPC(bool fromHost, bool isBack)
    {
        if (fromHost && isHostReady) return;
        if (!fromHost && isClientReady) return;

        if (fromHost)
        {
            if (isBack)
            {
                _currentHostType--;
                if (_currentHostType < 0)
                    _currentHostType = 2;
            }
            else
            {
                _currentHostType++;
                if (_currentHostType > 2)
                    _currentHostType = 0;
            }
            
            SessionHub.Instance.ChangeType(true, _currentHostType);
        }
        else
        {
            if (isBack)
            {
                _currentClientType--;
                if (_currentClientType < 0)
                    _currentClientType = 2;
            }
            else
            {
                _currentClientType++;
                if (_currentClientType > 2)
                    _currentClientType = 0;
            }

            SessionHub.Instance.ChangeType(false, _currentClientType);
        }

        UpdateTypeToAll_RPC(_currentHostType, _currentClientType);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void UpdateTypeToAll_RPC(int hostType, int clientType)
    {
        SessionHub.Instance.ChangeType(true, hostType);
        SessionHub.Instance.ChangeType(false, clientType);
    }

    public void UpdateCode(string code)
    {
        if (HasStateAuthority)
        {
            ServerCode = code;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void CodeAnnouncement_RPC()
    {
        SessionHub.Instance.UpdateCode(ServerCode);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void UpdateType_RPC()
    {
        if (HasStateAuthority)
        {
            currentHost = SessionHub.Instance.hostType;
            currentClient = SessionHub.Instance.clientType;
        }
    }
}
