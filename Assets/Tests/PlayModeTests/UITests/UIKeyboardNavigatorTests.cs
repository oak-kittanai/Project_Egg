using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using TMPro;
using Assert = NUnit.Framework.Assert;

public class UIKeyboardNavigatorTests
{
    private GameObject root;
    private EventSystem eventSystem;
    private Button buttonUp, buttonDown;
    private Slider slider;
    private TMP_InputField inputField;
    private bool buttonUpClicked;
    private InputSettings.UpdateMode originalUpdateMode;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        originalUpdateMode = InputSystem.settings.updateMode;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        ReleaseAllKeys();

        root = new GameObject("TestRoot");
        root.AddComponent<Canvas>();
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        var esGO = new GameObject("EventSystem");
        eventSystem = esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();
        esGO.AddComponent<UIKeyboardNavigator>();

        var btnUpGO = new GameObject("ButtonUp", typeof(RectTransform));
        btnUpGO.transform.SetParent(root.transform);
        btnUpGO.transform.localPosition = new Vector3(0, 100, 0);
        buttonUp = btnUpGO.AddComponent<Button>();
        buttonUpClicked = false;
        buttonUp.onClick.AddListener(() => buttonUpClicked = true);

        var btnDownGO = new GameObject("ButtonDown", typeof(RectTransform));
        btnDownGO.transform.SetParent(root.transform);
        btnDownGO.transform.localPosition = new Vector3(0, -100, 0);
        buttonDown = btnDownGO.AddComponent<Button>();

        var navUp = buttonUp.navigation;
        navUp.mode = Navigation.Mode.Explicit;
        navUp.selectOnDown = buttonDown;
        buttonUp.navigation = navUp;

        var navDown = buttonDown.navigation;
        navDown.mode = Navigation.Mode.Explicit;
        navDown.selectOnUp = buttonUp;
        buttonDown.navigation = navDown;

        var sliderGO = new GameObject("Slider", typeof(RectTransform));
        sliderGO.transform.SetParent(root.transform);
        slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0.5f;

        var inputGO = new GameObject("InputField", typeof(RectTransform));
        inputGO.transform.SetParent(root.transform);

        var textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(inputGO.transform);
        var textAreaRect = textArea.GetComponent<RectTransform>();

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(textArea.transform);
        var tmpText = textGO.AddComponent<TextMeshProUGUI>();

        inputField = inputGO.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = tmpText;

        eventSystem.SetSelectedGameObject(buttonUp.gameObject);

        Debug.Log($"[Test Setup] EventSystem ทั้งหมดที่ active ในซีนนี้: {Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length} ตัว");
        Debug.Log($"[Test Setup] EventSystem.current == ของเทสต์เราเอง? {ReferenceEquals(EventSystem.current, eventSystem)}");

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        ReleaseAllKeys();
        InputSystem.settings.updateMode = originalUpdateMode;
        if (root != null) Object.Destroy(root);
        if (eventSystem != null) Object.Destroy(eventSystem.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PressEnter_OnSelectedButton_InvokesOnClick()
    {
        eventSystem.SetSelectedGameObject(buttonUp.gameObject);
        yield return null;

        AssertEventSystemNotShadowed();
        PressKey(Key.Enter);
        yield return null;
        ReleaseAllKeys();
        yield return null;

        Assert.IsTrue(buttonUpClicked, "กด Enter บนปุ่มที่เลือกอยู่ ต้องเรียก onClick ผ่าน UIKeyboardNavigator");
    }

    [UnityTest]
    public IEnumerator PressS_MovesSelectionToNextButton()
    {
        eventSystem.SetSelectedGameObject(buttonUp.gameObject);
        yield return null;

        AssertEventSystemNotShadowed();
        PressKey(Key.S);
        yield return null;
        ReleaseAllKeys();
        yield return null;

        Assert.AreEqual(buttonDown.gameObject, eventSystem.currentSelectedGameObject,
            "กด S ต้องเลื่อน selection ไปปุ่มถัดไปตาม Navigation (ผ่าน UIKeyboardNavigator จริง)");
    }

    [UnityTest]
    public IEnumerator PressD_OnSelectedSlider_IncreasesValue()
    {
        eventSystem.SetSelectedGameObject(slider.gameObject);
        yield return null;
        float before = slider.value;

        AssertEventSystemNotShadowed();
        PressKey(Key.D);
        yield return null;
        ReleaseAllKeys();
        yield return null;

        Assert.Greater(slider.value, before, "กด D บน Slider ที่เลือกอยู่ ต้องเพิ่มค่า");
    }

    [UnityTest]
    public IEnumerator WhileTypingInInputField_WASDIsNotIntercepted()
    {
        eventSystem.SetSelectedGameObject(inputField.gameObject);
        inputField.Select();
        inputField.ActivateInputField();
        yield return null;

        Assert.IsTrue(inputField.isFocused, "ต้อง activate สำเร็จก่อน — ถ้า false แปลว่า setup เทสต์ผิด ไม่ใช่บั๊กจริง");

        AssertEventSystemNotShadowed();
        PressKey(Key.S); // เลียนแบบผู้เล่นพิมพ์ตัวอักษร "s" เป็นส่วนหนึ่งของโค้ดห้อง
        yield return null;
        ReleaseAllKeys();
        yield return null;

        Assert.AreEqual(inputField.gameObject, eventSystem.currentSelectedGameObject,
            "ตอนกำลังพิมพ์ใน TMP_InputField ต้องไม่ถูก WASD แย่ง selection ไป (regression ของบั๊กที่เคยแก้)");
    }

    private void AssertEventSystemNotShadowed()
    {
        Assert.IsTrue(ReferenceEquals(EventSystem.current, eventSystem),
            $"EventSystem.current ไม่ตรงกับของเทสต์ — มี EventSystem อื่นแอบ active อยู่ใน Editor session นี้ (current={EventSystem.current?.name ?? "null"})");
    }

    private static void PressKey(Key key)
    {
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
        InputSystem.Update();
    }

    private static void ReleaseAllKeys()
    {
        if (Keyboard.current == null) return;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        InputSystem.Update();
    }
}
