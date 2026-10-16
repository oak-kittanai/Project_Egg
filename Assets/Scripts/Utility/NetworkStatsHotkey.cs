using System.Reflection;
using Fusion;
using Fusion.Statistics;
using UnityEngine;
using UnityEngine.InputSystem;

// เครื่องมือวัด network (NJ-0): กด F11 เปิด/ปิดแผง Fusion Statistics ของ runner ที่กำลังรัน
// ทำงานเฉพาะใน Editor และ Development Build — ไม่ต้องแก้ NetworkRunner.prefab
public class NetworkStatsHotkey : MonoBehaviour
{
    // ค่าที่ใช้เทียบก่อน/หลังแต่ละเฟส: ping, ปริมาณข้อมูล, จำนวน tick ที่ client ย้อนคำนวณ, จำนวน object ที่อัปเดต
    private const RenderSimStats DefaultStats =
        RenderSimStats.RTT |
        RenderSimStats.InBandwidth |
        RenderSimStats.OutBandwidth |
        RenderSimStats.Resimulations |
        RenderSimStats.ForwardTicks |
        RenderSimStats.InObjectUpdates |
        RenderSimStats.OutObjectUpdates;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) return;

        var go = new GameObject(nameof(NetworkStatsHotkey));
        DontDestroyOnLoad(go);
        go.AddComponent<NetworkStatsHotkey>();
    }

    private bool _showConnectionInfo;
    private GUIStyle _labelStyle;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
            _showConnectionInfo = Toggle();
    }

    private static NetworkRunner FindRunningRunner()
    {
        foreach (var r in NetworkRunner.Instances)
        {
            if (r != null && r.IsRunning) return r;
        }
        return null;
    }

    // คืนค่า true ถ้าแผงเปิดอยู่หลังกด
    private static bool Toggle()
    {
        NetworkRunner runner = FindRunningRunner();

        if (runner == null)
        {
            Debug.Log("[NetStats] F11: no running NetworkRunner (start or join a session first)");
            return false;
        }

        if (!runner.TryGetComponent<FusionStatistics>(out var stats))
        {
            stats = runner.gameObject.AddComponent<FusionStatistics>();
            SelectDefaultStats(stats);
        }

        if (stats.IsPanelActive)
        {
            stats.DestroyStatisticsPanel();
            return false;
        }

        stats.SetupStatisticsPanel();
        return true;
    }

    // ต่อตรง (Direct) หรือผ่าน relay ของ Photon — ผ่าน relay ping จะสูงกว่า เป็นสาเหตุ lag ที่แก้ในโค้ดไม่ได้
    private void OnGUI()
    {
        if (!_showConnectionInfo) return;

        NetworkRunner runner = FindRunningRunner();
        if (runner == null) { _showConnectionInfo = false; return; }

        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14 };
            _labelStyle.normal.textColor = Color.white;
        }

        string text;
        if (runner.IsServer)
        {
            text = "Host";
            foreach (var player in runner.ActivePlayers)
            {
                if (player == runner.LocalPlayer) continue;
                text += $"\nClient {player.PlayerId}: {runner.GetPlayerConnectionType(player)} | RTT {runner.GetPlayerRtt(player) * 1000.0:0} ms";
            }
        }
        else
        {
            text = $"Client: {runner.CurrentConnectionType} | RTT {runner.GetPlayerRtt(runner.LocalPlayer) * 1000.0:0} ms";
        }

        GUI.Label(new Rect(10, 10, 320, 60), text, _labelStyle);
    }

    // Fusion ไม่มี API สาธารณะสำหรับเลือกค่าที่แสดง — ตั้ง field ที่ serialize อยู่ก่อนสร้างแผง
    // ถ้าเวอร์ชันของ Fusion เปลี่ยนชื่อ field ให้เลือกค่าเองในแผง (ปุ่มตั้งค่าของ Fusion Statistics)
    private static void SelectDefaultStats(FusionStatistics stats)
    {
        var field = typeof(FusionStatistics).GetField("_statsEnabled", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(RenderSimStats))
            field.SetValue(stats, DefaultStats);
        else
            Debug.LogWarning("[NetStats] could not preselect stats — pick them in the Fusion Statistics panel");
    }
}
