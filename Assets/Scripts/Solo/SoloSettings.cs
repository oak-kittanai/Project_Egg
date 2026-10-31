using UnityEngine;

// ค่าปรับของโหมด Solo — แก้ที่ไฟล์ Resources/SoloSettings ใน Inspector ถ้าไม่มีไฟล์ใช้ค่าเริ่มต้นด้านล่าง
[CreateAssetMenu(fileName = "SoloSettings", menuName = "Settings/Solo Settings")]
public class SoloSettings : ScriptableObject
{
    [Tooltip("Cooldown สกิลเพื่อน นกพาบิน (วินาที)")]
    public float assistCooldown = 6f;
    [Tooltip("ระยะที่ตัวตามหลังเดินห่างจากตัวหลัก (หน่วย)")]
    public float followDistance = 1.2f;
    [Tooltip("ขึ้นจากน้ำแล้วตัวตามหลังอยู่ไกลเกินนี้ให้วาร์ปมาหลังตัวหลัก (หน่วย)")]
    public float shoreWarpDistance = 8f;
    [Tooltip("ความเร็วตอนตัวตามหลังเดินตามให้ทัน (หน่วย/วิ)")]
    public float catchUpSpeed = 7f;
    [Tooltip("กันกดสลับตัวรัว (วินาที)")]
    public float switchCooldown = 0.3f;

    private static SoloSettings loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = null;
    }

    public static SoloSettings Instance
    {
        get
        {
            if (loaded == null) loaded = Resources.Load<SoloSettings>("SoloSettings");
            if (loaded == null) loaded = CreateInstance<SoloSettings>();
            return loaded;
        }
    }
}
