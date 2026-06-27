using System.Text;
using Fusion;
using UnityEngine;

// Debug overlay แสดง ping/RTT + NAT Type + Connection Type (Direct/Relay) ตอนเล่นใน session
// ใช้เช็คปัญหา NAT / Relay Server:
//   - Connection Type = Direct  -> ต่อตรง peer-to-peer (ping ต่ำ)
//   - Connection Type = Relay   -> ต่อตรงไม่ได้ (NAT ปิด) ต้องผ่าน Photon Relay Server (ping สูงขึ้น)
//
// วิธีใช้: สร้าง GameObject เปล่าในซีน gameplay แล้ว Add Component นี้ (หรือแปะบน object ที่มีอยู่)
// มันจะ auto-find NetworkRunner ที่กำลังรันเอง — ใช้ได้ทั้งฝั่ง Host และ Client
public class NetworkDebugOverlay : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool showOnScreen = true;
    [SerializeField] private bool logToConsole = false;
    [Tooltip("รีเฟรชค่าทุกกี่วินาที")]
    [SerializeField] private float refreshInterval = 0.5f;
    [SerializeField] private int fontSize = 18;

    private NetworkRunner runner;
    private float timer;
    private string cachedText = "[Net] waiting for runner...";
    private GUIStyle labelStyle;

    private void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = refreshInterval;

        if (runner == null || !runner.IsRunning)
            runner = FindRunningRunner();

        cachedText = BuildText();
        if (logToConsole) Debug.Log(cachedText);
    }

    private NetworkRunner FindRunningRunner()
    {
        var runners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        foreach (var r in runners)
            if (r != null && r.IsRunning) return r;
        return null;
    }

    private string BuildText()
    {
        if (runner == null || !runner.IsRunning)
            return "[Net] no running runner";

        var sb = new StringBuilder();
        string role = runner.IsServer ? "HOST / SERVER" : (runner.IsClient ? "CLIENT" : "?");
        sb.AppendLine($"[Net] Role: {role}");
        sb.AppendLine($"NAT Type: {runner.NATType}");

        if (runner.IsServer)
        {
            int shown = 0;
            foreach (var p in runner.ActivePlayers)
            {
                if (p == runner.LocalPlayer) continue;

                double rttMs = runner.GetPlayerRtt(p) * 1000.0;
                var conn = runner.GetPlayerConnectionType(p); // server-only API
                sb.AppendLine($"  Client {p.PlayerId}: {rttMs:F0} ms  [{conn}]");
                shown++;
            }
            if (shown == 0) sb.AppendLine("  (ยังไม่มี client เชื่อมต่อ)");
        }
        else // client
        {
            double rttMs = runner.GetPlayerRtt(PlayerRef.None) * 1000.0; // RTT ไปหา server
            sb.AppendLine($"Ping -> Server: {rttMs:F0} ms");
            sb.AppendLine($"Connection Type: {runner.CurrentConnectionType}");
            sb.AppendLine($"Connected To Server: {runner.IsConnectedToServer}");
        }

        return sb.ToString();
    }

    private void OnGUI()
    {
        if (!showOnScreen) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.UpperLeft,
                richText = false,
            };
            labelStyle.normal.textColor = Color.white;
        }

        var content = new GUIContent(cachedText);
        Vector2 size = labelStyle.CalcSize(content);
        var bg = new Rect(8, 8, size.x + 16, size.y + 10);

        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(bg, Texture2D.whiteTexture);
        GUI.color = prev;

        GUI.Label(new Rect(bg.x + 8, bg.y + 5, bg.width, bg.height), content, labelStyle);
    }
}
