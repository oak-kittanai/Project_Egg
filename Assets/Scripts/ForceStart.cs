using Fusion;
using UnityEngine;

public class ForceStart : NetworkBehaviour
{
    NetworkRunner runner;
    [SerializeField] NetworkObject PlayerSpawner;
    [SerializeField] GameObject NetworkRunner;

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

        ForceStartGame();
    }

    private void ForceStartGame()
    {
        StartSession(runner);
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
            await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = "Testsever",
                PlayerCount = 2
            });

            NetworkObject goPlayerSpawner = runner.Spawn(PlayerSpawner);
            PlayerSpawn playerSpawn = goPlayerSpawner.GetComponent<PlayerSpawn>();
            playerSpawn.runner = runner;
        }

    }
}
