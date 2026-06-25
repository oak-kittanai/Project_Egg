using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Fusion;
using Assert = NUnit.Framework.Assert;

public class TrapCheckIntegrationTests
{
    private string trapPath = "Prefabs/Traps_Prefabs/BearTrap";
    private NetworkObject trapObject;
    private BearTrapScript trapScript;

    // Network
    private NetworkRunner runner;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        var runnerObj = new GameObject("TestRunner");
        runner = runnerObj.AddComponent<NetworkRunner>();
        runner.ProvideInput = false;
        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Single,
            SessionName = "TrapTest_" + System.Guid.NewGuid(),
            SceneManager = sceneManager
        };

        var startTask = runner.StartGame(startArgs);
        yield return new WaitUntil(() => startTask.IsCompleted);
        Assert.IsTrue(startTask.Result.Ok, $"NetworkRunner start failed: {startTask.Result.ShutdownReason}");

        var trapObjectPath = Resources.Load<NetworkObject>(trapPath);
        Assert.IsNotNull(trapObjectPath, $"ไม่พบ prefab ที่ Resources/{trapPath}");
        trapObject = runner.Spawn(trapObjectPath, Vector3.zero, Quaternion.identity);

        yield return null;
        Assert.IsNotNull(trapObject, "trapObject is null");

        trapScript = trapObject.GetComponent<BearTrapScript>();
        Assert.IsNotNull(trapScript, "TrapScript is null");
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        if (runner != null && runner.IsRunning)
        {
            if (trapObject != null) runner.Despawn(trapObject);
            yield return null;
        }

        if (runner != null)
        {
            runner.Shutdown();
            yield return new WaitUntil(() => runner.IsShutdown);
            Object.DestroyImmediate(runner.gameObject);
        }
    }

    [UnityTest]
    public IEnumerator CheckAudioFunction()
    {
        // ARRANGE

        // ACT

        yield return new WaitForSeconds(2f);

        // ASSERT
    }
}
