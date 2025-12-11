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

    [Networked] public bool startAble { get; set; }

    public override void Spawned()
    {
        CodeAnnouncement_RPC();
    }

    public override void FixedUpdateNetwork()
    {
        SessionHub.Instance.UpdateOverTime();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void ChangeTypeRequest_RPC(bool fromHost)
    {
        if (fromHost)
        {
            _currentHostType++;
            if (_currentHostType > 2)
                _currentHostType = 0;

            SessionHub.Instance.ChangeType(true, _currentHostType);
        }
        else
        {
            _currentClientType++;
            if (_currentClientType > 2)
                _currentClientType = 0;

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
