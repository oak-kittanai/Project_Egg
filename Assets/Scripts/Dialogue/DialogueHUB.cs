using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DialogueHUB : MonoBehaviour
{
    public static DialogueHUB Instance { get; private set; }

    [SerializeField] private GameObject dialogueObject;
    [SerializeField] private GameObject dialogueBG;

    [SerializeField] private GameObject miraBox;
    [SerializeField] private GameObject kaelBox;
    [SerializeField] private CustomTextGen miraAnimator;
    [SerializeField] private CustomTextGen kaelAnimator;

    [SerializeField] Button nextButton;
    [SerializeField] Button prevButton;

    // สถานะโหวตปิด dialogue (FB2-6) — สร้างด้วยโค้ดใต้ dialogueObject ไม่ต้องแก้ Canvas prefab
    private TextMeshProUGUI voteStatusText;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject.transform.root.gameObject);
    }

    public void SetButton()
    {
        if (nextButton == null || prevButton == null) return;

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("[DialogueHUB] DialogueManager.Instance is null — SetButton skipped");
            return;
        }

        nextButton.onClick.RemoveAllListeners();
        prevButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(DialogueManager.Instance.NextLine);
        prevButton.onClick.AddListener(DialogueManager.Instance.PreviousLine);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        FindUIReferences();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIReferences();
    }

    public void FindUIReferences()
    {
        Transform root = transform.root;
        bool foundFromRoot = TryFindFromRoot(root);

        if (foundFromRoot)
        {
            SetButton();
            return;
        }
        TryFindFromScene(root);
    }

    private bool TryFindFromRoot(Transform root)
    {
        Transform dialogueParent = root.Find("Canvas");
        if (dialogueParent == null) return false;

        Transform dialogueObj = dialogueParent.Find("Dialogue");
        if (dialogueObj == null)
        {
            dialogueObj = dialogueParent;
        }

        dialogueObject = dialogueObj.gameObject;
        dialogueBG = dialogueObj.Find("BG")?.gameObject;

        miraBox = dialogueObj.Find("MiraBox")?.gameObject;
        kaelBox = dialogueObj.Find("KaelBox")?.gameObject;

        if (miraBox != null) miraAnimator = miraBox.GetComponentInChildren<CustomTextGen>();
        if (kaelBox != null) kaelAnimator = kaelBox.GetComponentInChildren<CustomTextGen>();

        nextButton = dialogueObj.Find("NextButton")?.GetComponent<Button>();
        prevButton = dialogueObj.Find("PrevButton")?.GetComponent<Button>();

        CloseDialogue(); // ปิดไว้ก่อนตั้งแต่เริ่ม จนกว่าจะมี DisplayLine จริง

        return dialogueObject != null;
    }

    private void TryFindFromScene(Transform root)
    {
        GameObject uiCanvas = GameObject.Find("Canvas");

        if (uiCanvas == null)
        {
            Debug.LogWarning("[DialogueHUB] can't find DialogueCanvas");
            return;
        }

        dialogueObject = uiCanvas.transform.Find("Dialogue")?.gameObject;
        if (dialogueObject == null) return;

        dialogueBG = dialogueObject.transform.Find("BG")?.gameObject;
        miraBox = dialogueObject.transform.Find("MiraBox")?.gameObject;
        kaelBox = dialogueObject.transform.Find("KaelBox")?.gameObject;

        if (miraBox != null) miraAnimator = miraBox.GetComponentInChildren<CustomTextGen>();
        if (kaelBox != null) kaelAnimator = kaelBox.GetComponentInChildren<CustomTextGen>();

        nextButton = dialogueObject.transform.Find("NextButton")?.GetComponent<Button>();
        prevButton = dialogueObject.transform.Find("PrevButton")?.GetComponent<Button>();

        CloseDialogue(); // ปิดไว้ก่อนตั้งแต่เริ่ม จนกว่าจะมี DisplayLine จริง

        SetButton();
    }

    public void DisplayLine(string speaker, string message, TextEffectType effect)
    {
        if (dialogueObject == null) { Debug.LogWarning("[DialogueHUB] dialogueObject is null"); return; }

        dialogueObject.SetActive(true);
        if (dialogueBG != null) dialogueBG.SetActive(true);
        if (miraBox != null) miraBox.SetActive(speaker == "Mira");
        if (kaelBox != null) kaelBox.SetActive(speaker == "Kael");

        if (speaker == "Mira" && miraAnimator != null) miraAnimator.StartEffect(message, effect);
        else if (speaker == "Kael" && kaelAnimator != null) kaelAnimator.StartEffect(message, effect);

        if (EventSystem.current != null && nextButton != null)
            EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
    }

    // ข้อความเล็กมุมขวาล่างของกล่อง ข้างปุ่มเลื่อนบรรทัด — null/ว่าง = ซ่อน
    public void SetVoteStatus(string message)
    {
        bool show = !string.IsNullOrEmpty(message);

        if (show && dialogueObject != null &&
            (voteStatusText == null || voteStatusText.transform.parent != dialogueObject.transform))
        {
            if (voteStatusText != null) Destroy(voteStatusText.gameObject);
            voteStatusText = CreateVoteStatusText();
        }

        if (voteStatusText == null) return;

        if (voteStatusText.gameObject.activeSelf != show) voteStatusText.gameObject.SetActive(show);
        if (show && voteStatusText.text != message) voteStatusText.text = message;
    }

    private TextMeshProUGUI CreateVoteStatusText()
    {
        var go = new GameObject("VoteStatus", typeof(RectTransform));
        go.transform.SetParent(dialogueObject.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(600f, 36f);
        rt.anchoredPosition = new Vector2(-300f, 118f);

        var text = go.AddComponent<TextMeshProUGUI>();
        GameObject box = miraBox != null ? miraBox : kaelBox;
        TextMeshProUGUI source = box != null ? box.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (source != null)
        {
            text.font = source.font;
            text.fontSharedMaterial = source.fontSharedMaterial;
        }
        text.fontSize = 22;
        text.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        go.SetActive(false);

        return text;
    }

    public void CloseDialogue()
    {
        if (voteStatusText != null) voteStatusText.gameObject.SetActive(false);
        if (dialogueObject != null) dialogueObject.SetActive(false);
        if (dialogueBG != null) dialogueBG.SetActive(false);
        if (miraBox != null) miraBox.SetActive(false);
        if (kaelBox != null) kaelBox.SetActive(false);
    }

    private void OnDestroy()
    {
        DesetButton();
    }

    public void DesetButton()
    {
        if (nextButton != null) nextButton.onClick.RemoveAllListeners();
        if (prevButton != null) prevButton.onClick.RemoveAllListeners();
    }
}