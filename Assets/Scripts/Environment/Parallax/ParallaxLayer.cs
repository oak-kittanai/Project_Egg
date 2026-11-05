using UnityEngine;

// BG-1: ชั้น parallax หนึ่งชั้น — ParallaxBackground ที่เป็นพ่อเป็นคนสั่งย้าย
// factor 0 = ติดกับโลก (ระนาบที่เล่น), 1 = ติดกับกล้อง (ไกลสุด), ติดลบ = foreground (เลื่อนเร็วกว่าโลก)
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("แกน X: 0 = เลื่อนพร้อมพื้น, 0.9 = ไกลมาก (เกือบตามกล้อง), ติดลบ = foreground หน้าตัวละคร")]
    public float parallaxFactor;

    [Tooltip("แกน Y: ปกติให้น้อยกว่าแกน X; 0 = ไม่ขยับขึ้นลงตามกล้อง")]
    public float parallaxFactorY;

    private Vector3 basePosition;
    private bool hasBase;

    private void Awake()
    {
        CaptureBase();
    }

    private void CaptureBase()
    {
        if (hasBase) return;
        basePosition = transform.localPosition;
        hasBase = true;
    }

    // cameraOffset = ตำแหน่งกล้องตอนนี้ ลบ ตำแหน่งอ้างอิงของด่าน (ตำแหน่งที่ชั้นวางไว้ใน Scene view ถูกต้องพอดี)
    public void Apply(Vector2 cameraOffset)
    {
        CaptureBase();
        transform.localPosition = basePosition + new Vector3(cameraOffset.x * parallaxFactor, cameraOffset.y * parallaxFactorY, 0f);
    }
}
