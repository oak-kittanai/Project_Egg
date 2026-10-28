using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// บอกว่า Controller เลือกอะไรอยู่ (HL-1/HL-3):
// - ปุ่ม/ช่องพิมพ์ที่มีรูป/สีตอนเมาส์ชี้ (Hover) -> ใช้หน้าตา Hover นั้นตอนถูกเลือก
// - หลอดปรับค่า และปุ่ม/ช่องที่ไม่มี Hover ให้เห็น -> กรอบขาวมุมมนรอบตัว
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
    private Selectable currentSelectable;
    private bool currentInteractable;

    // ปุ่มที่กำลังยืมหน้าตา Hover มาเป็นหน้าตาตอนถูกเลือก + ค่าเดิมไว้คืน (HL-3)
    // uGUI โชว์สถานะ Selected ก่อน Highlighted เสมอ จึงต้องสลับค่า Selected ชั่วคราวแทนการจำลองเมาส์ชี้
    private Selectable hoverTarget;
    private SpriteState savedSpriteState;
    private ColorBlock savedColors;

    // สี Hover ต้องต่างจากสีปกติอย่างน้อยเท่านี้ (ช่องใดช่องหนึ่ง) ถึงนับว่ามองเห็น — ค่าเริ่มต้นของ Unity 0.96 ต่างแค่ 0.04
    private const float VisibleTintDelta = 0.08f;

    private void LateUpdate()
    {
        // แสดงกรอบเฉพาะตอนกำลังใช้จอย (อุปกรณ์ที่กดล่าสุด) — ใช้เมาส์/คีย์บอร์ดกรอบหาย แตะจอยกรอบขึ้น (HL-2)
        bool usingGamepad = InputDeviceWatcher.UsingGamepad;

        var selected = usingGamepad && EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null && !selected.activeInHierarchy) selected = null;

        if (selected != currentSelected)
        {
            currentSelected = selected;
            currentSelectable = selected != null ? selected.GetComponent<Selectable>() : null;
            currentInteractable = currentSelectable != null && currentSelectable.IsInteractable();
            HandleSelectionChanged();
        }
        else if (currentSelectable != null && currentSelectable.IsInteractable() != currentInteractable)
        {
            // ปุ่มเปลี่ยนกดได้/กดไม่ได้ระหว่างถูกเลือก (เช่นปุ่ม Start ตอนอีกคนกด Ready) -> เลือก Hover/กรอบใหม่
            currentInteractable = !currentInteractable;
            HandleSelectionChanged();
        }

        UpdateHighlightTransform();
    }

    private void OnDisable()
    {
        RestoreHover();
    }

    private void HandleSelectionChanged()
    {
        RestoreHover();

        var targetRect = currentSelected != null ? currentSelected.GetComponent<RectTransform>() : null;
        if (targetRect == null || (currentSelectable != null && TryApplyHover(currentSelectable)))
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

    // ปุ่ม/ช่องพิมพ์ที่มี Hover ให้เห็น: ใช้รูป/สี Hover เป็นหน้าตาตอนถูกเลือก (เก็บค่าเดิมไว้คืน) — หลอดใช้กรอบเสมอ
    // ปุ่มที่กดไม่ได้ Unity โชว์หน้าตา Disabled ทับ Selected -> ใช้กรอบ
    private bool TryApplyHover(Selectable selectable)
    {
        if (!selectable.IsInteractable()) return false;
        if (selectable is Slider || selectable is Scrollbar) return false;

        switch (selectable.transition)
        {
            case Selectable.Transition.SpriteSwap:
                SpriteState sprites = selectable.spriteState;
                if (sprites.highlightedSprite == null) return false;

                savedSpriteState = sprites;
                sprites.selectedSprite = sprites.highlightedSprite;
                selectable.spriteState = sprites;
                break;

            case Selectable.Transition.ColorTint:
                ColorBlock colors = selectable.colors;
                if (!VisiblyDifferent(colors.highlightedColor, colors.normalColor)) return false;

                savedColors = colors;
                colors.selectedColor = colors.highlightedColor;
                selectable.colors = colors;
                break;

            default:
                return false;
        }

        hoverTarget = selectable;
        return true;
    }

    // คืนหน้าตาตอนถูกเลือกเดิม — ไม่งั้นปุ่มที่เมาส์คลิกภายหลังจะค้างหน้า Hover
    private void RestoreHover()
    {
        if (hoverTarget != null)
        {
            if (hoverTarget.transition == Selectable.Transition.SpriteSwap) hoverTarget.spriteState = savedSpriteState;
            else hoverTarget.colors = savedColors;
        }
        hoverTarget = null;
    }

    private static bool VisiblyDifferent(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) >= VisibleTintDelta
            || Mathf.Abs(a.g - b.g) >= VisibleTintDelta
            || Mathf.Abs(a.b - b.b) >= VisibleTintDelta;
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
