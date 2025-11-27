using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class ForceStart : NetworkBehaviour
{
    NetworkRunner runner;
    [SerializeField] NetworkObject PlayerSpawner;
    [SerializeField] GameObject NetworkRunner;

    [SerializeField] Button ForceStartButton;

    private void Awake()
    {
        GameObject goRunner = Instantiate(NetworkRunner);
        goRunner.name = "RunnerHost";
        runner = goRunner.GetComponent<NetworkRunner>();

        if (runner != null)
        {
            Debug.Log("Successful spawn runner");
        }
        else Debug.Log("Can't spawn runner");

        ForceStartButton.onClick.AddListener(ForceStartGame);
    }

    private void OnDestroy()
    {
        ForceStartButton.onClick.RemoveAllListeners();
    }

    private void ForceStartGame()
    {
        StartSession(runner);
        Debug.Log("Start Session");
    }

    public async void StartSession(NetworkRunner runner)
    {
        if (runner == null)
        {
            Debug.Log("can't start seesion find runner");
            return;
        }
        else
        {
            runner.ProvideInput = true;
            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(0));

            var res = await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = "TestServer",
                PlayerCount = 20
            });
            if (!res.Ok)
            {
                Debug.LogError($"Shutdown reason is {res.ShutdownReason}");
            }
            else
            {
                Debug.Log("Success create room");
            }

            if (runner.IsServer)
            {
                NetworkObject goPlayerSpawner = runner.Spawn(PlayerSpawner);
                PlayerSpawn playerSpawn = goPlayerSpawner.GetComponent<PlayerSpawn>();
                INetworkStructure networkStructure = runner.GetComponent<INetworkStructure>();
                playerSpawn.runner = runner;
                networkStructure.spawner = playerSpawn;
                Debug.Log("Spawn PlayerSpawner");
            }

            ForceStartButton.gameObject.SetActive(false);
        }
    }
}
