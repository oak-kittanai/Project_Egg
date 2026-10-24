// TEMP — test branches only. DO NOT MERGE.
// PF-2: หาสาเหตุที่เกมหน่วงขึ้นเรื่อย ๆ เมื่อเล่นนาน — log ค่าที่อาจโตตามเวลาทุก 30 วิ ทั้ง Host และ Client (อ่านจาก Editor.log / Player.log)
// กด F12 แสดงค่าล่าสุดบนจอ
using Fusion;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

public class VerifyPerfMonitor : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("[VERIFY] PerfMonitor");
        DontDestroyOnLoad(go);
        go.AddComponent<VerifyPerfMonitor>();
    }

    private const float Interval = 30f;
    private const float HitchMs = 50f;

    private float _windowStart;
    private float _startTime;
    private int _frames;
    private float _sumMs;
    private float _maxMs;
    private int _hitches;
    private int _gcAtWindowStart;
    private int _logs;
    private bool _show;
    private string _last = "perf: waiting for first sample (30s)";

    // หน่วยความจำที่จองต่อเฟรม (Editor และ development build) — ยิ่งมากยิ่งเกิด GC บ่อย
    private ProfilerRecorder _gcAllocRecorder;
    private long _allocBytes;

    private void OnEnable()
    {
        Application.logMessageReceived += CountLog;
        _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= CountLog;
        _gcAllocRecorder.Dispose();
    }
    private void CountLog(string condition, string stackTrace, UnityEngine.LogType type) => _logs++;

    private void Start()
    {
        _startTime = _windowStart = Time.realtimeSinceStartup;
        _gcAtWindowStart = System.GC.CollectionCount(0);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame) _show = !_show;

        float ms = Time.unscaledDeltaTime * 1000f;
        _frames++;
        _sumMs += ms;
        if (ms > _maxMs) _maxMs = ms;
        if (ms > HitchMs) _hitches++;
        if (_gcAllocRecorder.Valid) _allocBytes += _gcAllocRecorder.LastValue;

        if (Time.realtimeSinceStartup - _windowStart >= Interval) Sample();
    }

    private void Sample()
    {
        int gcNow = System.GC.CollectionCount(0);

        int audioPlaying = 0;
        var audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var a in audioSources) if (a.isPlaying) audioPlaying++;

        int particlesAlive = 0;
        var particles = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in particles) if (p.IsAlive(false)) particlesAlive++;

        _last =
            $"perf role={VerifyLog.Role()} up={(Time.realtimeSinceStartup - _startTime) / 60f:F1}min " +
            $"frame avg={_sumMs / Mathf.Max(1, _frames):F1}ms max={_maxMs:F0}ms hitches>{HitchMs:F0}ms={_hitches} " +
            $"gc={gcNow - _gcAtWindowStart} allocKB/frame={(_gcAllocRecorder.Valid ? (_allocBytes / 1024f / Mathf.Max(1, _frames)).ToString("F1") : "n/a")} monoMB={Profiler.GetMonoUsedSizeLong() / 1048576f:F1} heapMB={Profiler.GetMonoHeapSizeLong() / 1048576f:F1} allocMB={Profiler.GetTotalAllocatedMemoryLong() / 1048576f:F0} " +
            $"objects={FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length} " +
            $"netObjs={FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length} " +
            $"rb2d={FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None).Length} " +
            $"audio={audioSources.Length}/{audioPlaying}playing particles={particles.Length}/{particlesAlive}alive " +
            $"materials={Resources.FindObjectsOfTypeAll<Material>().Length} textures={Resources.FindObjectsOfTypeAll<Texture2D>().Length} " +
            $"tweens={DG.Tweening.DOTween.TotalActiveTweens()} logs={_logs}";

        VerifyLog.Log(_last);

        _windowStart = Time.realtimeSinceStartup;
        _frames = 0;
        _sumMs = 0f;
        _maxMs = 0f;
        _hitches = 0;
        _allocBytes = 0;
        _gcAtWindowStart = gcNow;
    }

    private void OnGUI()
    {
        if (!_show) return;
        GUI.Label(new Rect(10, 10, Screen.width - 20, 80), _last);
    }
}
