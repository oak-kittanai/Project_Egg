using UnityEngine;
using UnityEngine.UI;

// รูปปุ่มที่เปลี่ยนตามอุปกรณ์ (IP-1): ใส่บน Image หรือ SpriteRenderer แล้วเลือก action
// คีย์บอร์ด/เมาส์ = รูปที่ตั้งไว้บนชิ้นนั้นอยู่แล้ว (art เดิมไม่เปลี่ยน; ไม่มีรูปเดิม -> ใช้ช่อง keyboard ใน InputGlyphSet)
// จอย = รูปจาก InputGlyphSet ตามยี่ห้อ
public class InputGlyph : MonoBehaviour
{
    [SerializeField] private InputPromptAction action;
    [Tooltip("ใช้รูปที่ตั้งไว้บน Image/SpriteRenderer เป็นรูปตอนใช้คีย์บอร์ด")]
    [SerializeField] private bool keepAuthoredKeyboardSprite = true;

    private Image image;
    private SpriteRenderer spriteRenderer;
    private Sprite authoredSprite;
    private Vector3 authoredScale;
    private float authoredHeight;
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
        spriteRenderer = GetComponent<SpriteRenderer>();
        authoredSprite = image != null ? image.sprite : spriteRenderer != null ? spriteRenderer.sprite : null;
        authoredScale = transform.localScale;
        authoredHeight = authoredSprite != null ? authoredSprite.bounds.size.y : 0f;
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
        Sprite sprite = PickSprite();
        if (sprite == null) return;

        if (image != null) image.sprite = sprite;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            // ขนาดบนจอเท่ารูปเดิม แม้ art ใหม่จะ PPU/ขอบรูปต่างกัน
            float height = sprite.bounds.size.y;
            transform.localScale = authoredHeight > 0f && height > 0f ? authoredScale * (authoredHeight / height) : authoredScale;
        }
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
