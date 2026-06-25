using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;

public class TutorialPanelKeyboardNavTests
{
    private GameObject canvasRoot;
    private EventSystem eventSystem;
    private TutorialUIManager tutorialUIManager;
    private Button prevButton, nextButton;
    private List<TutorialData> testList;
    private InputSettings.UpdateMode originalUpdateMode;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        TutorialUIManager.Instance = null; // กัน singleton ค้างจากเทสต์อื่น/รันก่อนหน้า

        originalUpdateMode = InputSystem.settings.updateMode;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        ReleaseAllKeys();

        canvasRoot = new GameObject("Canvas", typeof(RectTransform));
        canvasRoot.AddComponent<Canvas>();
        canvasRoot.AddComponent<CanvasScaler>();
        canvasRoot.AddComponent<GraphicRaycaster>();

        var esGO = new GameObject("EventSystem");
        eventSystem = esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();
        esGO.AddComponent<UIKeyboardNavigator>();

        // สร้าง TutorialPanel hierarchy ให้ตรงชื่อจริงตามที่ TutorialUIManager.RegisterCanvas ค้นหา
        var panelGO = new GameObject("TutorialPanel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasRoot.transform);

        var bgGO = new GameObject("BackGround", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(panelGO.transform);

        var tutorialBgGO = new GameObject("TutorialBackground", typeof(RectTransform), typeof(Image));
        tutorialBgGO.transform.SetParent(panelGO.transform);

        var prevGO = new GameObject("PrevButton", typeof(RectTransform));
        prevGO.transform.SetParent(panelGO.transform);
        prevButton = prevGO.AddComponent<Button>();

        var nextGO = new GameObject("NextButton", typeof(RectTransform));
        nextGO.transform.SetParent(panelGO.transform);
        nextButton = nextGO.AddComponent<Button>();

        var navPrev = prevButton.navigation;
        navPrev.mode = Navigation.Mode.Explicit;
        navPrev.selectOnRight = nextButton;
        prevButton.navigation = navPrev;

        var navNext = nextButton.navigation;
        navNext.mode = Navigation.Mode.Explicit;
        navNext.selectOnLeft = prevButton;
        nextButton.navigation = navNext;

        var sliderGO = new GameObject("SliderTimer", typeof(RectTransform));
        sliderGO.transform.SetParent(panelGO.transform);
        sliderGO.AddComponent<Slider>();

        var hintGO = new GameObject("PressTabToClose", typeof(RectTransform));
        hintGO.transform.SetParent(panelGO.transform);

        panelGO.SetActive(false); // ปิดไว้ก่อน เหมือนสภาพจริงตอนยังไม่เปิด tutorial

        var managerGO = new GameObject("TutorialUIManager");
        tutorialUIManager = managerGO.AddComponent<TutorialUIManager>();
        tutorialUIManager.RegisterCanvas(canvasRoot); // เช็คเส้นทาง "เปิดไฟล์เจอไหม" ตรงๆ

        testList = new List<TutorialData>
        {
            new TutorialData { tutorialName = "Test1", displayDuration = 0f },
            new TutorialData { tutorialName = "Test2", displayDuration = 0f },
        };

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        ReleaseAllKeys();
        InputSystem.settings.updateMode = originalUpdateMode;
        if (canvasRoot != null) Object.Destroy(canvasRoot);
        if (eventSystem != null) Object.Destroy(eventSystem.gameObject);
        if (tutorialUIManager != null) Object.Destroy(tutorialUIManager.gameObject);
        TutorialUIManager.Instance = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator RegisterCanvas_FindsTutorialPanel_AndShowSelectsNextButton()
    {
        tutorialUIManager.ShowTutorialPanel(testList, 0, isFirstTimeView: false);
        yield return null;

        Assert.IsTrue(tutorialUIManager.IsTutorialOpen,
            "RegisterCanvas ต้องหา TutorialPanel เจอ ไม่งั้น ShowTutorialPanel จะเปิด panel ไม่ได้เลย");
        Assert.AreEqual(nextButton.gameObject, eventSystem.currentSelectedGameObject,
            "ต้องหา NextButton เจอจาก RegisterCanvas และถูกตั้งเป็น default selection ตอนเปิด panel");
    }

    [UnityTest]
    public IEnumerator PressEnter_OnNextButton_AdvancesTutorial()
    {
        tutorialUIManager.ShowTutorialPanel(testList, 0, isFirstTimeView: false);
        yield return null;

        AssertEventSystemNotShadowed();
        PressKey(Key.Enter);
        yield return null;
        ReleaseAllKeys();
        yield return null;

        Assert.IsTrue(prevButton.interactable, "หลังกด Next จาก index 0 ไป index สุดท้าย ปุ่ม Prev ต้องกดได้แล้ว");
        Assert.IsFalse(nextButton.interactable, "อยู่ tutorial ตัวสุดท้ายแล้ว ปุ่ม Next ต้อง Not Available");
    }

    [UnityTest]
    public IEnumerator PressA_MovesSelectionToPrevButton()
    {
        tutorialUIManager.ShowTutorialPanel(testList, 1, isFirstTimeView: false);
        yield return null;

        AssertEventSystemNotShadowed();
        PressKey(Key.A);
        yield return null;
        ReleaseAllKeys();
        yield return null;

        Assert.AreEqual(prevButton.gameObject, eventSystem.currentSelectedGameObject,
            "กด A ต้องเลื่อน selection จาก NextButton ไป PrevButton ตาม Navigation");
    }

    [UnityTest]
    public IEnumerator WhileTutorialOpen_GameplayInputWouldBeLocked_ButUINaviStillWorks()
    {
        tutorialUIManager.ShowTutorialPanel(testList, 0, isFirstTimeView: false);
        yield return null;

        Assert.IsTrue(tutorialUIManager.IsTutorialOpen,
            "เงื่อนไขนี้ (IsTutorialOpen=true) คือตัวเดียวกับที่ INetworkStructure.OnInput ใช้ล็อค gameplay input — ต้องเป็น true ก่อนเช็คขั้นต่อไป");

        AssertEventSystemNotShadowed();
        PressKey(Key.Enter);
        yield return null;
        ReleaseAllKeys();
        yield return null;

        Assert.IsTrue(prevButton.interactable,
            "แม้ gameplay input จะถูกล็อค (IsTutorialOpen=true) UINavi ต้องยังกด NextButton ได้ตามปกติ เพราะ UIKeyboardNavigator เป็นเส้นทางคนละระบบกับ NetworkInputData/OnInput โดยสิ้นเชิง");
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
