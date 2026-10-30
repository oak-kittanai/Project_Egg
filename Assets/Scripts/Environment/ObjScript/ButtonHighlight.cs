using System;
using UnityEngine;

// เส้นขอบของปุ่มเหยียบ (BT-1): ขาว = เหยียบได้, เหลือง = ถูกเหยียบอยู่ — สีตาม IsPressed ที่ sync แล้ว ทุกเครื่องเห็นตรงกัน
// วาดด้วยเงารูปปุ่ม 8 ทิศไว้หลังปุ่ม เพราะรูปปุ่มชิดขอบ shader เส้นขอบจึงวาดออกนอกรูปไม่ได้
// เงาใช้ material ไม่รับแสง ไฟในฉากอมฟ้าจะได้ไม่ทำให้สีเพี้ยน
[Serializable]
public class ButtonHighlight
{
    private const string MaterialPath = "Materials/ButtonHighlight";

    private static readonly Vector2[] Offsets =
    {
        new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-1f, 1f),
        new Vector2(-1f, 0f), new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f),
    };

    private static Material material;

    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(1f, 0.84f, 0.1f);
    [Tooltip("ความหนาเส้นขอบ หน่วยเป็น pixel ของรูปปุ่ม")]
    [SerializeField] private float pixels = 2f;

    private SpriteRenderer[] outlines;
    private Sprite shownSprite;
    private bool shownPressed;

    public void Show(SpriteRenderer target, bool pressed)
    {
        if (target.sprite == null) return;

        if (outlines == null)
        {
            Build(target);
            shownPressed = !pressed; // ให้ตั้งสีครั้งแรกด้านล่าง
        }

        if (target.sprite != shownSprite)
        {
            shownSprite = target.sprite;
            float step = pixels / shownSprite.pixelsPerUnit;
            for (int i = 0; i < outlines.Length; i++)
            {
                outlines[i].sprite = shownSprite;
                outlines[i].transform.localPosition = Offsets[i] * step;
            }
        }

        if (pressed != shownPressed)
        {
            shownPressed = pressed;
            Color color = pressed ? pressedColor : idleColor;
            for (int i = 0; i < outlines.Length; i++) outlines[i].color = color;
        }
    }

    private void Build(SpriteRenderer target)
    {
        if (material == null) material = Resources.Load<Material>(MaterialPath);
        if (material == null)
        {
            outlines = new SpriteRenderer[0];
            return;
        }

        outlines = new SpriteRenderer[Offsets.Length];
        for (int i = 0; i < outlines.Length; i++)
        {
            var outline = new GameObject("Highlight").AddComponent<SpriteRenderer>();
            outline.gameObject.layer = target.gameObject.layer;
            outline.transform.SetParent(target.transform, false);
            outline.sharedMaterial = material;
            outline.sortingLayerID = target.sortingLayerID;
            outline.sortingOrder = target.sortingOrder - 1;
            outline.flipX = target.flipX;
            outline.flipY = target.flipY;
            outlines[i] = outline;
        }
    }
}
