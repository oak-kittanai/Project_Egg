// TEMP — branch test/verify only. DO NOT MERGE.
// Logs + debug keys used to confirm the bugs listed in GDD_AUDIT.md (C-1, H-1, H-2, H-4, M-2, M-4).
using System.Collections;
using System.Text;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class VerifyLog
{
    public static void Log(string msg)
    {
        Debug.Log($"[VERIFY] [{Role()}] [{SceneManager.GetActiveScene().name}] {msg}");
    }

    public static NetworkRunner FindRunner()
    {
        foreach (var r in NetworkRunner.Instances)
        {
            if (r != null && r.IsRunning) return r;
        }
        return null;
    }

    public static string Role()
    {
        var r = FindRunner();
        if (r == null) return "NO-RUNNER";
        return r.IsServer ? "HOST" : "CLIENT";
    }

    // Root canvases still alive (name + owning scene) — used for H-1 (leftover HUD in lobby).
    public static string RootCanvases()
    {
        var sb = new StringBuilder();
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.transform.parent != null) continue;
            sb.Append($"{c.gameObject.name}({c.gameObject.scene.name},active={c.gameObject.activeInHierarchy}) ");
        }
        return sb.Length == 0 ? "none" : sb.ToString();
    }

    public static string Statics()
    {
        return $"IsDialogueOpen={DialogueManager.IsDialogueOpen} " +
               $"birdFly={PlayerInterface._birdFlyUnlocked} birdThrow={PlayerInterface._birdThrowUnlocked} " +
               $"duckDive={PlayerInterface._duckDiveUnlocked} duckSmash={PlayerInterface._duckSmashUnlocked}";
    }
}

public class VerifyDebugKeys : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("[VERIFY] DebugKeys");
        DontDestroyOnLoad(go);
        go.AddComponent<VerifyDebugKeys>();
        VerifyLog.Log("DebugKeys ready (F6 = log marker, F7 = to Passel Key, F8 = into warp, F9 = force lobby, F10 = drop connection)");
    }

    private static int _markCount;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        VerifyLog.Log($"sceneLoaded '{scene.name}' | {VerifyLog.Statics()} | rootCanvases: {VerifyLog.RootCanvases()}");
        StartCoroutine(LogAfterDelay(scene.name));
    }

    private IEnumerator LogAfterDelay(string sceneName)
    {
        yield return new WaitForSecondsRealtime(1f);
        VerifyLog.Log($"1s after sceneLoaded '{sceneName}' | {VerifyLog.Statics()} | rootCanvases: {VerifyLog.RootCanvases()} " +
                      $"| SessionManager.Instance null={SessionManager.Instance == null} SessionHub.Instance null={SessionHub.Instance == null}");
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f6Key.wasPressedThisFrame)
        {
            _markCount++;
            VerifyLog.Log($"========== MARK {_markCount} ========== | {VerifyLog.Statics()} | rootCanvases: {VerifyLog.RootCanvases()}");
        }
        if (kb.f7Key.wasPressedThisFrame) TeleportToPasselKey();
        if (kb.f8Key.wasPressedThisFrame) StartCoroutine(WarpBothPlayers());
        if (kb.f9Key.wasPressedThisFrame) ForceReturnToLobby();
        if (kb.f10Key.wasPressedThisFrame) DropConnection();
    }

    private static bool TryGetHostRunner(string key, out NetworkRunner runner)
    {
        runner = VerifyLog.FindRunner();
        if (runner != null && runner.IsServer) return true;

        VerifyLog.Log($"{key} ignored: host only (press it in the Host window)");
        return false;
    }

    private static void TeleportPlayers(Vector3 center)
    {
        float offset = -0.3f;
        foreach (var mc in FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None))
        {
            if (!mc.enabled || mc.Object == null || !mc.Object.IsValid) continue;

            Vector3 pos = center + new Vector3(offset, 0.2f, 0f);
            offset += 0.6f;

            if (mc.TryGetComponent<NetworkRigidbody2D>(out var netRb))
                netRb.Teleport(pos, mc.transform.rotation);

            VerifyLog.Log($"teleported {(mc.isBird ? "Bird" : "Duck")} to {pos}");
        }
    }

    // F7 (host): bring both players next to the first Passel Key (StoneItem) of the level.
    private void TeleportToPasselKey()
    {
        if (!TryGetHostRunner("F7", out _)) return;

        var keys = FindObjectsByType<StoneItem>(FindObjectsSortMode.None);
        if (keys.Length == 0)
        {
            VerifyLog.Log("F7: no StoneItem (Passel Key) left in this scene");
            return;
        }

        VerifyLog.Log($"F7: teleporting players to '{keys[0].name}'");
        TeleportPlayers(keys[0].transform.position);
    }

    // F8 (host): put both players inside the level's WarpZone so the real OnTriggerEnter2D path runs.
    private IEnumerator WarpBothPlayers()
    {
        if (!TryGetHostRunner("F8", out _)) yield break;

        var warp = FindFirstObjectByType<WarpZone>();
        if (warp == null)
        {
            VerifyLog.Log("F8: no WarpZone in this scene");
            yield break;
        }

        // S3: the final door only enables its collider once InsertStone_Mech.IsOpen is true.
        if (warp.TryGetComponent<InsertStone_Mech>(out var door) && !door.IsOpen)
        {
            VerifyLog.Log("F8: opening final door (InsertStone_Mech.IsOpen = true)");
            door.IsOpen = true;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        Vector3 target = warp.transform.position;
        if (warp.TryGetComponent<Collider2D>(out var col) && col.enabled) target = col.bounds.center;

        VerifyLog.Log($"F8: teleporting players into WarpZone '{warp.name}' at {target}");
        TeleportPlayers(target);
    }

    // F9: call the leave path the code intends (GameManager.ReturnToSession) when stuck in a dead stage.
    private async void ForceReturnToLobby()
    {
        var runner = VerifyLog.FindRunner();
        VerifyLog.Log($"F9: force return to lobby (runner running={runner != null})");

        if (runner != null) await runner.Shutdown();
        GameManager.ReturnToSession();
    }

    // F10: shut down this window's runner to simulate this side dropping out.
    private void DropConnection()
    {
        var runner = VerifyLog.FindRunner();
        if (runner == null)
        {
            VerifyLog.Log("F10: no running runner");
            return;
        }

        VerifyLog.Log("F10: runner.Shutdown() — simulating this side dropping");
        _ = runner.Shutdown();
    }
}
