using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ป้ายปุ่มของโหมด Solo — สร้างตอนรันเพื่อไม่แก้ Canvas.prefab ของ Duo ใช้ข้อความจนกว่า UI จะมีรูป RT / R2 / คีย์ Y
public class SoloHud : MonoBehaviour
{
    private static SoloHud instance;

    private TMP_Text prompt;
    private TMP_Text hint;
    private float hintUntil;

    public static void Create(Transform parent)
    {
        var root = new GameObject("SoloHud", typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(parent, false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        instance = root.AddComponent<SoloHud>();
        instance.prompt = instance.CreateText("Prompt", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 24f), TextAlignmentOptions.BottomLeft, 30f);
        instance.hint = instance.CreateText("Hint", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), TextAlignmentOptions.Center, 42f);
    }

    public static void ShowHint(string text, float seconds)
    {
        if (instance == null) return;
        instance.hint.text = text;
        instance.hintUntil = Time.unscaledTime + seconds;
    }

    private TMP_Text CreateText(string objectName, Vector2 anchor, Vector2 pivot, Vector2 position, TextAlignmentOptions alignment, float size)
    {
        var obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(transform, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(1200f, 120f);

        var text = obj.AddComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.fontSize = size;
        text.raycastTarget = false;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        return text;
    }

    private void Update()
    {
        if (hint.text.Length > 0 && Time.unscaledTime > hintUntil) hint.text = "";

        SoloParty party = SoloParty.Instance;
        if (party == null || party.Active == null)
        {
            prompt.text = "";
            return;
        }

        string switchKey, liftKey;
        switch (InputDeviceWatcher.Current)
        {
            case InputScheme.Xbox: switchKey = "RT"; liftKey = "Y"; break;
            case InputScheme.PlayStation: switchKey = "R2"; liftKey = "TRIANGLE"; break;
            default: switchKey = "R"; liftKey = "Y"; break;
        }

        string text = $"{switchKey}: SWITCH";
        if (party.Active == party.Duck) text += $"    {liftKey}: LIFT BIRD";

        float cooldown = party.AssistCooldownLeft;
        if (cooldown > 0f) text += $"    BIRD LIFT {Mathf.CeilToInt(cooldown)}s";

        prompt.text = text;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
