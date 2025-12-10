using Fusion;
using UnityEngine;

public class RuntimeUpdate : SingletonNetwork<RuntimeUpdate>
{
    public NetworkRunner runner;

    [Networked] bool _isBirdTaken { get; set; }
    [Networked] bool _isDuckTaken { get; set; }

    [Networked] bool ChangeCharacter { get; set; }

    [Networked] int _currentHostType { get; set; }
    [Networked] int _currentClientType { get; set; }

    public override void Spawned()
    {

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

            SessionHub.Instance.ChangeType(1, _currentHostType);
        }
        else
        {
            _currentClientType++;
            if (_currentClientType > 2)
                _currentClientType = 0;

            SessionHub.Instance.ChangeType(2, _currentClientType);
        }

        UpdateTypeToAll_RPC(_currentHostType, _currentClientType);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void UpdateTypeToAll_RPC(int hostType, int clientType)
    {
        SessionHub.Instance.ChangeType(1, hostType);
        SessionHub.Instance.ChangeType(2, clientType);
    }
}
