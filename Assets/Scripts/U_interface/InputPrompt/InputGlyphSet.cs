using System;
using UnityEngine;

// รูป/ชื่อปุ่มของแต่ละการกระทำ แยกตามอุปกรณ์ (IP-1) — ทีม UI แก้ที่ไฟล์ Resources/InputGlyphSet ใน Inspector
// component ทุกตัวโหลดไฟล์นี้เอง ไม่ต้องลากต่อทีละชิ้น
[CreateAssetMenu(fileName = "InputGlyphSet", menuName = "Settings/Input Glyph Set")]
public class InputGlyphSet : ScriptableObject
{
    [Serializable]
    public class Glyph
    {
        public Sprite sprite;
        [Tooltip("รูปตอนกดค้าง (ว่าง = ใช้รูปปกติ)")]
        public Sprite pressed;
        [Tooltip("ชื่อปุ่มในข้อความ เช่น E, X, Square — ใช้ตัว ASCII เท่านั้น (ฟอนต์ในเกมไม่มี ✕○□△)")]
        public string label;

        public bool IsEmpty => sprite == null && string.IsNullOrEmpty(label);
    }

    [Serializable]
    public class Entry
    {
        public InputPromptAction action;
        public Glyph keyboard = new Glyph();
        public Glyph xbox = new Glyph();
        [Tooltip("ว่าง = ใช้ของ Xbox")]
        public Glyph playStation = new Glyph();
    }

    public Entry[] entries = new Entry[0];

    private static InputGlyphSet loaded;
    private static bool triedLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = null;
        triedLoad = false;
    }

    // null = ไม่มีไฟล์ใน Resources -> component ใช้รูปคีย์บอร์ดเดิมของตัวเอง
    public static InputGlyphSet Instance
    {
        get
        {
            if (loaded == null && !triedLoad)
            {
                triedLoad = true;
                loaded = Resources.Load<InputGlyphSet>("InputGlyphSet");
            }
            return loaded;
        }
    }

    public Glyph Get(InputPromptAction action, InputScheme scheme)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (entry.action != action) continue;

            switch (scheme)
            {
                case InputScheme.PlayStation:
                    return entry.playStation != null && !entry.playStation.IsEmpty ? entry.playStation : entry.xbox;
                case InputScheme.Xbox:
                    return entry.xbox;
                default:
                    return entry.keyboard;
            }
        }
        return null;
    }

    public string Label(InputPromptAction action, InputScheme scheme)
    {
        Glyph glyph = Get(action, scheme);
        return glyph != null && !string.IsNullOrEmpty(glyph.label) ? glyph.label : action.ToString();
    }
}
