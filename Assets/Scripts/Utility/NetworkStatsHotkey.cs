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

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
            Toggle();
    }

    private static void Toggle()
    {
        NetworkRunner runner = null;
        foreach (var r in NetworkRunner.Instances)
        {
            if (r != null && r.IsRunning) { runner = r; break; }
        }

        if (runner == null)
        {
            Debug.Log("[NetStats] F11: no running NetworkRunner (start or join a session first)");
            return;
        }

        if (!runner.TryGetComponent<FusionStatistics>(out var stats))
        {
            stats = runner.gameObject.AddComponent<FusionStatistics>();
            SelectDefaultStats(stats);
        }

        if (stats.IsPanelActive) stats.DestroyStatisticsPanel();
        else stats.SetupStatisticsPanel();
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
