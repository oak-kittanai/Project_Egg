using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class ForceStart : NetworkBehaviour
{
    NetworkRunner runner;
    [SerializeField] NetworkObject PlayerPrefabs;
    [SerializeField] GameObject NetworkRunner;

    [SerializeField] NetworkObject CenterHost;

    [SerializeField] Button ForceStartButton;
    [SerializeField] InputField ServerIdInsert;
    [SerializeField] string ServerId = "ServerTest";

    private void Awake()
    {
        GameObject goRunner = Instantiate(NetworkRunner);
        goRunner.name = "RunnerHost";
        runner = goRunner.GetComponent<NetworkRunner>();

        if (runner != null)
        {
            Debug.Log("Successful spawn runner");
            runner = FindFirstObjectByType<NetworkRunner>();
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
        StartSession(runner, ServerId);
        Debug.Log("Start Session");
    }

    public async void StartSession(NetworkRunner runner, string ServerId)
    {
        if (runner == null)
        {
            Debug.Log("can't start seesion find runner");
            return;
        }
        else
        {
            runner.ProvideInput = true;

            string serverName = ServerId;
            StartGameArgs StartServer = new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = serverName,
                PlayerCount = 20,
            };

            var res = await runner.StartGame(StartServer);
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
                //PlayerSpawn playerSpawn = goPlayerSpawner.GetComponent<PlayerSpawn>();
                /*playerSpawn.runner = runner;
                networkStructure.spawner = playerSpawn;*/

                INetworkStructure networkStructure = runner.GetComponent<INetworkStructure>();

                NetworkObject noCenter = runner.Spawn(CenterHost);
                CenterHost CH = noCenter.GetComponent<CenterHost>();
                CH.AddComponent(runner, networkStructure, PlayerPrefabs);
            }

            ForceStartButton.gameObject.SetActive(false);
        }
    }
}
