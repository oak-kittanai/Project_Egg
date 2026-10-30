using System;
using UnityEngine;

// รูปหน้าสอนเล่นแยกตามอุปกรณ์ (IP-8) — ทีม UI แก้ที่ไฟล์ Resources/TutorialSpriteSet ใน Inspector
// ฉากเก็บแค่รูปคีย์บอร์ดใน LevelData แล้วจับคู่รูปอุปกรณ์อื่นจากตารางนี้ ทุกด่านจึงใช้ชุดเดียวกัน
[CreateAssetMenu(fileName = "TutorialSpriteSet", menuName = "Settings/Tutorial Sprite Set")]
public class TutorialSpriteSet : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public Sprite keyboard;
        public Sprite xbox;
        public Sprite playStation;
    }

    public Entry[] entries = new Entry[0];

    private static TutorialSpriteSet loaded;
    private static bool triedLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = null;
        triedLoad = false;
    }

    private static TutorialSpriteSet Instance
    {
        get
        {
            if (loaded == null && !triedLoad)
            {
                triedLoad = true;
                loaded = Resources.Load<TutorialSpriteSet>("TutorialSpriteSet");
            }
            return loaded;
        }
    }

    // รูปของอุปกรณ์นั้น — ไม่มีในตารางหรือยังไม่ใส่รูป คืนรูปคีย์บอร์ดที่ส่งมา
    public static Sprite Resolve(Sprite keyboard, InputScheme scheme)
    {
        if (keyboard == null || scheme == InputScheme.KeyboardMouse || Instance == null) return keyboard;

        foreach (Entry entry in Instance.entries)
        {
            if (entry.keyboard != keyboard) continue;

            Sprite sprite = scheme == InputScheme.Xbox ? entry.xbox : entry.playStation;
            return sprite != null ? sprite : keyboard;
        }
        return keyboard;
    }
}
