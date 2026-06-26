using System.Collections;
using Fusion;
using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private GameSessionData sessionData;
    [NetworkPrefab][SerializeField] private NetworkObject centerHostPrefab;
    [NetworkPrefab][SerializeField] private NetworkObject gameManagerPrefab;
    [NetworkPrefab][SerializeField] private NetworkObject dialogueVoteManagerPrefab;
    [NetworkPrefab][SerializeField] private NetworkObject birdPrefab;
    [NetworkPrefab][SerializeField] private NetworkObject duckPrefab;

    public void Bootstrap(NetworkRunner runner)
    {
        StartCoroutine(BootstrapRoutine(runner));
    }

    private IEnumerator BootstrapRoutine(NetworkRunner runner)
    {
        if (!runner.IsServer) yield break;

        INetworkStructure netStructure = runner.GetComponent<INetworkStructure>();

        // Step 1: Spawn CenterHost, wait for Spawned() to run (creates Canvas + PlayerInterface)
        runner.Spawn(centerHostPrefab);
        yield return new WaitUntil(() => CenterHost.Instance != null);
        yield return null; // one frame for Spawned() → SetupGameCore()

        CenterHost ch = CenterHost.Instance;
        ch.AddComponent(runner, netStructure, birdPrefab, duckPrefab);

        // Step 2: Spawn GameManager, wait for it to be registered
        runner.Spawn(gameManagerPrefab);
        yield return new WaitUntil(() => GameManager.Instance != null);

        GameManager.Instance.GetNetworkRunner(runner);

        // Step 2.5: Spawn DialogueVoteManager (votes-to-close ของ Dialogue)
        runner.Spawn(dialogueVoteManagerPrefab);

        // Step 3: Spawn Players — Canvas is ready, loading screen covers them
        MovementCharacter[] existingPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);
        if (existingPlayers.Length > 0)
        {
            yield break;
        }

        characterType hostType   = sessionData != null ? sessionData.hostType   : characterType.Duck;
        characterType clientType = sessionData != null ? sessionData.clientType : characterType.Bird;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            bool isHost = (player == runner.LocalPlayer);
            ch.SpawnPlayer(player, isHost ? hostType : clientType, isHost);
        }
    }
}
