using UnityEngine;
using UnityEngine.UI;

// แถบบอกปุ่มมุมขวาล่างของเมนู (BK-2) — ใช้รูปป้ายทั้งแถบจากทีม UI (UI-5)
// ขึ้นเฉพาะตอนใช้จอย; เลือกป้ายตามปุ่มย้อนกลับ (UIBackButton) ของหน้าที่เปิดอยู่:
//   ไม่มี = A TO CONFIRM, กดทีเดียว = … B TO BACK, กดค้าง = … B TO HOLD LEAVE
// รูป Xbox/PS เลือกโดย InputGlyph บนป้าย (InputGlyphSet ช่อง HintConfirm*) — ตัวนี้แค่ซ่อน/แสดง เลือกป้าย และวาดวงตอนกด B ค้าง
public class ControllerHintBar : MonoBehaviour
{
    [Tooltip("ตัวแถบ (ลูกของ object นี้) — object นี้ต้องเปิดอยู่ตลอดเพื่อเช็คทุกเฟรม")]
    [SerializeField] private GameObject content;
    [Tooltip("รูปป้ายทั้งแถบ (Image + InputGlyph) — สูงคงที่ กว้างตามสัดส่วนรูป")]
    [SerializeField] private InputGlyph banner;
    [Tooltip("วงรอบปุ่ม B ในป้าย Leave (Image แบบ Filled) เต็มตามเวลาที่กด B ค้าง — วางเป็นลูกของป้ายตรงปุ่ม B; ไม่มีรูปจะสร้างวงให้ตอนรัน")]
    [SerializeField] private Image holdRing;
    [Tooltip("ว่าง = แสดงตลอด; ใส่หน้าต่าง = แสดงเฉพาะตอนมีหน้าต่างใดเปิดอยู่ (เช่นเมนู pause ในเกม)")]
    [SerializeField] private GameObject[] onlyWhileOpen;

    private static Sprite ringSprite;

    private void Awake()
    {
        if (holdRing != null && holdRing.sprite == null) holdRing.sprite = RingSprite();
    }

    private void LateUpdate()
    {
        bool show = InputDeviceWatcher.UsingGamepad && AnyWindowOpen();
        if (content != null && content.activeSelf != show) content.SetActive(show);
        if (!show) return;

        UIBackButton back = UIBackButton.Current;
        InputPromptAction action = back == null ? InputPromptAction.HintConfirm
                                 : back.HoldSeconds > 0f ? InputPromptAction.HintConfirmLeave
                                 : InputPromptAction.HintConfirmBack;
        if (banner != null && banner.Action != action) banner.Action = action;

        if (holdRing != null)
        {
            bool holdable = action == InputPromptAction.HintConfirmLeave;
            if (holdRing.gameObject.activeSelf != holdable) holdRing.gameObject.SetActive(holdable);
            holdRing.fillAmount = UIBackButton.HoldProgress;
        }
    }

    private bool AnyWindowOpen()
    {
        if (onlyWhileOpen == null || onlyWhileOpen.Length == 0) return true;
        foreach (GameObject window in onlyWhileOpen)
            if (window != null && window.activeInHierarchy) return true;
        return false;
    }

    // วงแหวนขอบเรียบ (กลางโปร่ง) — ป้ายใหม่พื้นขาว วงทึบสีขาวแบบเดิมจะมองไม่เห็น
    private const int RingSize = 64;
    private const float RingThickness = 7f;

    private static Sprite RingSprite()
    {
        if (ringSprite != null) return ringSprite;

        var texture = new Texture2D(RingSize, RingSize, TextureFormat.RGBA32, false)
        {
            name = "HoldRing",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        float outer = RingSize / 2f - 1f;
        float inner = outer - RingThickness;
        var center = new Vector2(RingSize / 2f, RingSize / 2f);
        var pixels = new Color32[RingSize * RingSize];
        for (int y = 0; y < RingSize; y++)
        {
            for (int x = 0; x < RingSize; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(d - inner + 0.5f) * Mathf.Clamp01(outer - d + 0.5f);
                pixels[y * RingSize + x] = new Color32(255, 255, 255, (byte)(255f * alpha));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        ringSprite = Sprite.Create(texture, new Rect(0, 0, RingSize, RingSize), new Vector2(0.5f, 0.5f), 100f);
        return ringSprite;
    }
}
