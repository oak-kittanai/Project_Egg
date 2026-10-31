using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ป้ายปุ่มของโหมด Solo — สร้างตอนรันเพื่อไม่แก้ Canvas.prefab ของ Duo ใช้ข้อความจนกว่า UI จะมีรูป RT / R2 / คีย์ Y
public class SoloHud : MonoBehaviour
{
    private const float AboveDuckHeight = 1.5f;

    private static SoloHud instance;

    private TMP_Text prompt;
    private TMP_Text headPrompt;
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
        instance.headPrompt = instance.CreateText("HeadPrompt", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, TextAlignmentOptions.Bottom, 32f);
        instance.headPrompt.color = new Color(1f, 0.9f, 0.3f);
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
        text.text = ""; // TMP ที่เพิ่งสร้าง text เป็น null และ LateUpdate เฟรมแรกอ่านค่านี้
        text.alignment = alignment;
        text.fontSize = size;
        text.raycastTarget = false;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        return text;
    }

    // LateUpdate: ป้ายเหนือหัวต้องใช้ตำแหน่งภาพของเฟรมนี้
    private void LateUpdate()
    {
        if (hint.text.Length > 0 && Time.unscaledTime > hintUntil) hint.text = "";

        SoloParty party = SoloParty.Instance;
        if (party == null || party.Active == null)
        {
            prompt.text = "";
            headPrompt.text = "";
            return;
        }

        string switchKey, liftKey;
        switch (InputDeviceWatcher.Current)
        {
            case InputScheme.Xbox: switchKey = "RT"; liftKey = "Y"; break;
            case InputScheme.PlayStation: switchKey = "R2"; liftKey = "TRIANGLE"; break;
            default: switchKey = "R"; liftKey = "Y"; break;
        }

        Duck_Moveset duck = party.Duck;
        bool isDuck = party.Active == duck;
        bool lifting = isDuck && party.IsLifting;
        bool inWater = isDuck && (duck.isWaterSurface || duck.onDiving);
        bool canFly = lifting && party.Bird.isFlyUnlocked;
        bool birdInWater = !isDuck && (party.Bird.isWaterSurface || party.Bird.stilldrowning);

        float cooldown = party.AssistCooldownLeft;
        string waiting = $"BIRD FLY {Mathf.CeilToInt(cooldown)}s";
        string waterFly = cooldown > 0f ? waiting : $"{switchKey}: BIRD FLY";

        string corner;
        string head = "";

        if (isDuck && party.IsBirdSinking)
        {
            corner = party.BirdInReach ? $"{liftKey}: LIFT BIRD" : "SAVE THE BIRD";
            head = corner;
        }
        else if (lifting && duck.onDiving)
        {
            corner = "";
            head = "SWIM UP";
        }
        else if (!isDuck)
        {
            corner = !birdInWater ? $"{switchKey}: SWITCH" : duck.isDiveUnlocked ? $"{switchKey}: SWITCH TO DUCK" : "";
        }
        else if (inWater)
        {
            // บนน้ำปุ่มสลับคือสกิลเพื่อน ถ้าไม่ได้ยกนก ทั้งปุ่มสลับและปุ่มยกไม่มีผล
            corner = canFly ? waterFly : "";
            head = corner;
        }
        else
        {
            corner = $"{switchKey}: SWITCH    {liftKey}: {(lifting ? "PUT DOWN" : "LIFT BIRD")}";
            if (canFly && duck.IsInAir) head = cooldown > 0f ? "" : "JUMP: BIRD FLY";
            else if (canFly) head = cooldown > 0f ? waiting : "JUMP TWICE: BIRD FLY";
        }

        prompt.text = corner;
        headPrompt.text = head;
        if (head.Length > 0) PlaceAboveDuck(duck);
    }

    private void PlaceAboveDuck(Duck_Moveset duck)
    {
        Camera cam = CameraCharacter.LocalCamera;
        if (cam == null) return;

        Vector2 screen = cam.WorldToScreenPoint(duck.visualTransform.position + Vector3.up * AboveDuckHeight);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, null, out Vector2 local))
            headPrompt.rectTransform.anchoredPosition = local;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
