using UnityEngine;
using UnityEngine.UI;

// รูปปุ่มที่เปลี่ยนตามอุปกรณ์ (IP-1): ใส่บน Image, RawImage หรือ SpriteRenderer แล้วเลือก action
// คีย์บอร์ด/เมาส์ = รูปที่ตั้งไว้บนชิ้นนั้นอยู่แล้ว (art เดิมไม่เปลี่ยน; ไม่มีรูปเดิม -> ใช้ช่อง keyboard ใน InputGlyphSet)
// จอย = รูปจาก InputGlyphSet ตามยี่ห้อ
public class InputGlyph : MonoBehaviour
{
    [SerializeField] private InputPromptAction action;
    [Tooltip("ใช้รูปที่ตั้งไว้บน Image/RawImage/SpriteRenderer เป็นรูปตอนใช้คีย์บอร์ด")]
    [SerializeField] private bool keepAuthoredKeyboardSprite = true;

    private Image image;
    private RawImage rawImage;
    private SpriteRenderer spriteRenderer;
    private Sprite authoredSprite;
    private Texture authoredTexture;
    private Rect authoredUv;
    private Vector3 authoredScale;
    private float authoredHeight;
    private Vector2 authoredSize;
    private float referenceAspect;
    private bool pressed;
    private bool initialized;

    public InputPromptAction Action
    {
        get => action;
        set { action = value; Refresh(); }
    }

    private void Init()
    {
        if (initialized) return;
        initialized = true;

        image = GetComponent<Image>();
        rawImage = GetComponent<RawImage>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        authoredSprite = image != null ? image.sprite : spriteRenderer != null ? spriteRenderer.sprite : null;
        authoredTexture = rawImage != null ? rawImage.texture : null;
        authoredUv = rawImage != null ? rawImage.uvRect : new Rect(0f, 0f, 1f, 1f);
        authoredScale = transform.localScale;
        authoredHeight = authoredSprite != null ? authoredSprite.bounds.size.y : 0f;

        RectTransform rect = image != null ? image.rectTransform : rawImage != null ? rawImage.rectTransform : null;
        if (rect != null)
        {
            authoredSize = rect.sizeDelta;
            referenceAspect = authoredSprite != null ? SpriteAspect(authoredSprite)
                            : authoredTexture != null && authoredTexture.height > 0 && authoredUv.height > 0f
                                ? authoredTexture.width * authoredUv.width / (authoredTexture.height * authoredUv.height)
                            : authoredSize.y > 0f ? authoredSize.x / authoredSize.y : 1f;
        }
    }

    private void OnEnable()
    {
        Init();
        InputDeviceWatcher.Changed += OnSchemeChanged;
        Refresh();
    }

    private void OnDisable()
    {
        InputDeviceWatcher.Changed -= OnSchemeChanged;
    }

    private void OnSchemeChanged(InputScheme scheme) => Refresh();

    public void SetPressed(bool isPressed)
    {
        if (pressed == isPressed) return;
        pressed = isPressed;
        Refresh();
    }

    public void Refresh()
    {
        Init();

        // ป้ายที่เป็น RawImage (เช่น "PRESS Tab TO CLOSE") ตั้งไว้เป็น texture ไม่ใช่ sprite — คีย์บอร์ดคืน texture เดิม
        if (rawImage != null && InputDeviceWatcher.Current == InputScheme.KeyboardMouse && keepAuthoredKeyboardSprite && authoredTexture != null)
        {
            rawImage.texture = authoredTexture;
            rawImage.uvRect = authoredUv;
            rawImage.rectTransform.sizeDelta = authoredSize;
            return;
        }

        Sprite sprite = PickSprite();
        if (sprite == null) return;

        if (image != null)
        {
            image.sprite = sprite;
            FitWidthToSprite(image.rectTransform, authoredSize, referenceAspect, sprite);
        }
        if (rawImage != null)
        {
            // วาดเฉพาะส่วนของ sprite ใน texture (รูปที่ตัดขอบ/รวม atlas ก็ได้)
            Texture texture = sprite.texture;
            Rect r = sprite.textureRect;
            rawImage.texture = texture;
            rawImage.uvRect = new Rect(r.x / texture.width, r.y / texture.height, r.width / texture.width, r.height / texture.height);
            FitWidthToSprite(rawImage.rectTransform, authoredSize, referenceAspect, sprite);
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            // ขนาดบนจอเท่ารูปเดิม แม้ art ใหม่จะ PPU/ขอบรูปต่างกัน
            float height = sprite.bounds.size.y;
            transform.localScale = authoredHeight > 0f && height > 0f ? authoredScale * (authoredHeight / height) : authoredScale;
        }
    }

    // รูปปุ่มกว้างไม่เท่ากัน (R1 / ✕×2 / ป้าย TO CLOSE): สูงเท่าเดิม กว้างตามสัดส่วนรูป (IP-4)
    // เทียบกับรูปที่ช่องนี้ออกแบบไว้ (referenceAspect) — รูปเดิมจึงได้ขนาดเดิมพอดี และรูปที่ถูกยืดในช่องเดิมก็ยืดเท่ากัน
    // ช่องที่ยืดตามพ่อในแนวนอนไม่แตะ
    public static void FitWidthToSprite(RectTransform rect, Vector2 authoredSize, float referenceAspect, Sprite sprite)
    {
        if (rect == null || sprite == null || referenceAspect <= 0f) return;
        if (!Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x)) return;

        float aspect = SpriteAspect(sprite);
        if (aspect <= 0f) return;
        rect.sizeDelta = new Vector2(authoredSize.x * aspect / referenceAspect, authoredSize.y);
    }

    public static float SpriteAspect(Sprite sprite)
    {
        Rect r = sprite.rect;
        return r.height > 0f ? r.width / r.height : 0f;
    }

    private Sprite PickSprite()
    {
        InputScheme scheme = InputDeviceWatcher.Current;
        if (scheme == InputScheme.KeyboardMouse && keepAuthoredKeyboardSprite && authoredSprite != null) return authoredSprite;

        InputGlyphSet set = InputGlyphSet.Instance;
        InputGlyphSet.Glyph glyph = set != null ? set.Get(action, scheme) : null;
        if (glyph == null || glyph.sprite == null) return scheme == InputScheme.KeyboardMouse ? authoredSprite : null;
        return pressed && glyph.pressed != null ? glyph.pressed : glyph.sprite;
    }
}
