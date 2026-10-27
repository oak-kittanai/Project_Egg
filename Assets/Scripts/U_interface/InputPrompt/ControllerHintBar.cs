using TMPro;
using UnityEngine;
using UnityEngine.UI;

// แถบบอกปุ่มมุมขวาล่างของเมนู: [A] Confirm  [B] Back (BK-2)
// ขึ้นเฉพาะตอนใช้จอย; ส่วน B ขึ้นเมื่อหน้านั้นมีปุ่มย้อนกลับ (UIBackButton) และใช้คำของปุ่มนั้น
// รูปปุ่มเปลี่ยนตาม Xbox/PS ด้วย InputGlyph บนรูปแต่ละอัน — ตัวนี้แค่ซ่อน/แสดงและวาดวงตอนกด B ค้าง
public class ControllerHintBar : MonoBehaviour
{
    [Tooltip("ตัวแถบ (ลูกของ object นี้) — object นี้ต้องเปิดอยู่ตลอดเพื่อเช็คทุกเฟรม")]
    [SerializeField] private GameObject content;
    [SerializeField] private GameObject backGroup;
    [SerializeField] private TMP_Text backLabel;
    [Tooltip("วงรอบปุ่ม B (Image แบบ Filled) เต็มตามเวลาที่กด B ค้าง สำหรับปุ่มย้อนกลับแบบกดค้าง")]
    [SerializeField] private Image holdRing;
    [Tooltip("ว่าง = แสดงตลอด; ใส่หน้าต่าง = แสดงเฉพาะตอนมีหน้าต่างใดเปิดอยู่ (เช่นเมนู pause ในเกม)")]
    [SerializeField] private GameObject[] onlyWhileOpen;

    private void LateUpdate()
    {
        bool show = InputDeviceWatcher.UsingGamepad && AnyWindowOpen();
        if (content != null && content.activeSelf != show) content.SetActive(show);
        if (!show) return;

        UIBackButton back = UIBackButton.Current;
        bool hasBack = back != null;
        if (backGroup != null && backGroup.activeSelf != hasBack) backGroup.SetActive(hasBack);
        if (hasBack && backLabel != null && backLabel.text != back.Label) backLabel.text = back.Label;
        if (holdRing != null) holdRing.fillAmount = UIBackButton.HoldProgress;
    }

    private bool AnyWindowOpen()
    {
        if (onlyWhileOpen == null || onlyWhileOpen.Length == 0) return true;
        foreach (GameObject window in onlyWhileOpen)
            if (window != null && window.activeInHierarchy) return true;
        return false;
    }
}
