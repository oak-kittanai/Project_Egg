using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class ForceStart : NetworkBehaviour
{
    [SerializeField] INetworkStructure NetworkStructure;
    NetworkRunner runner;
    [SerializeField] GameObject PlayerSpawner;
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

            await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = "Testsever",
                PlayerCount = 2,
                Scene = sceneInfo,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            if (runner.IsServer)
            {
                GameObject goPlayerSpawner = Instantiate(PlayerSpawner);
                PlayerSpawn playerSpawn = goPlayerSpawner.GetComponent<PlayerSpawn>();
                playerSpawn.runner = runner;
                Debug.Log("Spawn PlayerSpawner");
            }

            ForceStartButton.gameObject.SetActive(false);
        }
    }
}
