using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// กรอบขาวมุมมนรอบปุ่มที่เลือกด้วย Controller (HL-1) — เดิมเป็นกล่องเหลืองโปร่งแสงทับปุ่ม
// รูปกรอบสร้างในโค้ด (ไม่ต้องมีไฟล์รูป); component นี้ถูกเพิ่มตอนรันโดย UIKeyboardNavigator จึงใช้ค่าเริ่มต้นด้านล่าง
public class SelectionHighlightFollower : MonoBehaviour
{
    [SerializeField] private Color highlightColor = Color.white;
    [SerializeField] private float padding = 6f;
    [Tooltip("ความหนาเส้นกรอบ (หน่วย UI ที่ความละเอียดอ้างอิงของ Canvas)")]
    [SerializeField] private int frameThickness = 4;
    [SerializeField] private int cornerRadius = 12;

    private static Sprite frameSprite;

    private RectTransform highlightRect;
    private Image highlightImage;
    private GameObject currentSelected;

    private void LateUpdate()
    {
        // แสดง highlight เฉพาะตอนมี gamepad เสียบอยู่ (เสียบ/ถอดระหว่างเล่นมีผลทันที)
        bool gamepadConnected = Gamepad.all.Count > 0;

        var selected = gamepadConnected && EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null && !selected.activeInHierarchy) selected = null;

        if (selected != currentSelected)
        {
            currentSelected = selected;
            HandleSelectionChanged();
        }

        UpdateHighlightTransform();
    }

    private void HandleSelectionChanged()
    {
        if (currentSelected == null)
        {
            if (highlightImage != null) highlightImage.gameObject.SetActive(false);
            return;
        }

        var targetRect = currentSelected.GetComponent<RectTransform>();
        if (targetRect == null)
        {
            if (highlightImage != null) highlightImage.gameObject.SetActive(false);
            return;
        }

        var targetCanvas = targetRect.GetComponentInParent<Canvas>();
        if (targetCanvas == null) return;

        EnsureHighlightExists();
        if (highlightRect.parent != targetCanvas.transform)
            highlightRect.SetParent(targetCanvas.transform, false);

        highlightRect.SetAsLastSibling();
        highlightImage.gameObject.SetActive(true);
    }

    private void EnsureHighlightExists()
    {
        if (highlightImage != null) return;

        var go = new GameObject("SelectionHighlight", typeof(RectTransform), typeof(Image));
        highlightRect = go.GetComponent<RectTransform>();
        highlightImage = go.GetComponent<Image>();
        highlightImage.sprite = FrameSprite(cornerRadius, frameThickness);
        highlightImage.type = Image.Type.Sliced;
        highlightImage.fillCenter = false;
        highlightImage.color = highlightColor;
        highlightImage.raycastTarget = false;
    }

    // วงขอบมุมมนสีขาว: มุมรัศมี radius, เส้นหนา thickness, กลางโปร่ง — ตัดแบบ 9-slice ให้ยืดตามขนาดปุ่มได้
    // ขอบเข้มบาง ๆ ด้านนอก/ด้านในเส้นขาว ให้เห็นบนปุ่มสีขาว/ฟ้าอ่อนของ lobby ด้วย
    private const int Rim = 2;
    private const float RimAlpha = 0.45f;
    private static readonly Color32 RimColor = new Color32(20, 24, 40, 255);

    private static Sprite FrameSprite(int radius, int thickness)
    {
        if (frameSprite != null) return frameSprite;

        radius = Mathf.Max(radius, thickness + Rim + 1);
        int outer = radius + Rim;
        int size = outer * 2 + 2;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SelectionFrame",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // ระยะจากจุดกลางมุมที่ใกล้ที่สุด (ช่วงขอบตรงเป็นระยะถึงเส้นขอบ) — d = outer คือขอบนอกสุด
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var corner = new Vector2(Mathf.Clamp(p.x, outer, size - outer), Mathf.Clamp(p.y, outer, size - outer));
                float d = Vector2.Distance(p, corner);

                float white = Band(d, outer - Rim - thickness, outer - Rim);
                float rim = Mathf.Max(Band(d, outer - Rim, outer), Band(d, outer - Rim - thickness - Rim, outer - Rim - thickness));
                float alpha = white + (1f - white) * rim * RimAlpha;
                Color32 c = Color32.Lerp(RimColor, new Color32(255, 255, 255, 255), white);
                c.a = (byte)(255f * alpha);
                pixels[y * size + x] = c;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        var border = new Vector4(outer, outer, outer, outer);
        frameSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        return frameSprite;
    }

    // ความทึบของวงระหว่างระยะ from..to (ขอบเรียบครึ่ง pixel)
    private static float Band(float d, float from, float to)
    {
        return Mathf.Clamp01(d - from + 0.5f) * Mathf.Clamp01(to - d + 0.5f);
    }

    private void UpdateHighlightTransform()
    {
        if (highlightImage == null || !highlightImage.gameObject.activeSelf || currentSelected == null) return;

        var targetRect = currentSelected.GetComponent<RectTransform>();
        if (targetRect == null) return;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        highlightRect.position = (corners[0] + corners[2]) / 2f;
        highlightRect.rotation = targetRect.rotation;

        float width = Vector3.Distance(corners[0], corners[3]);
        float height = Vector3.Distance(corners[0], corners[1]);
        float localScale = Mathf.Max(highlightRect.lossyScale.x, 0.0001f);

        highlightRect.sizeDelta = new Vector2(width, height) / localScale + new Vector2(padding, padding) * 2f;
    }
}
