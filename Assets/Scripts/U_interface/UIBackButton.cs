using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ปุ่มย้อนกลับของหน้านั้น — กด B บนจอยแล้วเหมือนกดปุ่มนี้ (BK-1); UIKeyboardNavigator เป็นคนอ่าน B
// ติดบนปุ่ม Back/X ของแต่ละหน้า ปุ่มเดิมยังเลื่อนไปกดเองได้เหมือนเดิม
[RequireComponent(typeof(Button))]
public class UIBackButton : MonoBehaviour
{
    [Tooltip("ต้องกด B ค้างกี่วินาทีถึงจะกดปุ่มนี้ (0 = กดทีเดียว) — ใช้กับปุ่มที่กดพลาดแล้วเสียหาย เช่นออกจากห้อง")]
    [SerializeField] private float holdSeconds;

    private static readonly List<UIBackButton> enabledButtons = new List<UIBackButton>();
    private static int consumedFrame = -1;

    private Button button;

    // แถบบอกปุ่มเลือกป้าย B TO BACK / B TO HOLD LEAVE จากค่านี้ (UI-5)
    public float HoldSeconds => holdSeconds;
    public Button Button => button != null ? button : button = GetComponent<Button>();

    // 0-1 ระหว่างกด B ค้างที่ปุ่มแบบกดค้าง — แถบบอกปุ่มวาดวงตามค่านี้
    public static float HoldProgress { get; internal set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        enabledButtons.Clear();
        consumedFrame = -1;
        HoldProgress = 0f;
    }

    private void OnEnable()
    {
        enabledButtons.Remove(this);
        enabledButtons.Add(this);
    }

    private void OnDisable() => enabledButtons.Remove(this);

    // ปุ่มย้อนกลับของหน้าที่เปิดอยู่: ตัวที่เปิดทีหลังสุดที่ยังกดได้และมองเห็น
    public static UIBackButton Current
    {
        get
        {
            for (int i = enabledButtons.Count - 1; i >= 0; i--)
            {
                UIBackButton candidate = enabledButtons[i];
                if (candidate != null && UIKeyboardNavigator.IsUsable(candidate.gameObject)) return candidate;
            }
            return null;
        }
    }

    // popup ที่ปิดตัวเองด้วย B (ปุ่มตัวเลข, หน้าสอนเล่น) เรียก — หน้าข้างหลังจะไม่ย้อนต่อในกดเดียวกัน
    public static void ConsumeBack() => consumedFrame = Time.frameCount;

    public static bool BackConsumedThisFrame => consumedFrame == Time.frameCount;
}
