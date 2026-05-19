using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DialogueHUB : MonoBehaviour
{
    public static DialogueHUB Instance { get; private set; }

    [SerializeField] private GameObject dialogueObject;

    [SerializeField] private GameObject miraBox;
    [SerializeField] private GameObject kaelBox;
    [SerializeField] private CustomTextGen miraAnimator;
    [SerializeField] private CustomTextGen kaelAnimator;

    [SerializeField] Button nextButton;
    [SerializeField] Button prevButton;

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
            Debug.Log("[DialogueHUB] found UI in Persistent Canvas");
            SetButton();
            return;
        }
        TryFindFromScene();
    }

    private bool TryFindFromRoot(Transform root)
    {
        Transform dialogueParent = root.Find("Dialogue");
        if (dialogueParent == null) return false;

        Transform dialogueObj = dialogueParent.Find("DialogueObject");
        if (dialogueObj == null)
        {
            dialogueObj = dialogueParent;
        }

        dialogueObject = dialogueObj.gameObject;

        miraBox = dialogueObj.Find("MiraBox")?.gameObject;
        kaelBox = dialogueObj.Find("KaelBox")?.gameObject;

        if (miraBox != null) miraAnimator = miraBox.GetComponent<CustomTextGen>();
        if (kaelBox != null) kaelAnimator = kaelBox.GetComponent<CustomTextGen>();

        nextButton = dialogueObj.Find("NextButton")?.GetComponent<Button>();
        prevButton = dialogueObj.Find("PrevButton")?.GetComponent<Button>();

        return dialogueObject != null;
    }

    private void TryFindFromScene()
    {
        GameObject uiCanvas = GameObject.Find("DialogueCanvas");

        if (uiCanvas == null)
        {
            Debug.LogWarning("[DialogueHUB] can't find DialogueCanvas");
            return;
        }

        dialogueObject = uiCanvas.transform.Find("DialogueObject")?.gameObject;
        if (dialogueObject == null) return;

        miraBox = dialogueObject.transform.Find("MiraBox")?.gameObject;
        kaelBox = dialogueObject.transform.Find("KaelBox")?.gameObject;

        if (miraBox != null) miraAnimator = miraBox.GetComponent<CustomTextGen>();
        if (kaelBox != null) kaelAnimator = kaelBox.GetComponent<CustomTextGen>();

        nextButton = dialogueObject.transform.Find("NextButton")?.GetComponent<Button>();
        prevButton = dialogueObject.transform.Find("PrevButton")?.GetComponent<Button>();

        SetButton();
        Debug.Log("[DialogueHUB] Found UI in Scene Canvas");
    }

    public void DisplayLine(string speaker, string message, TextEffectType effect)
    {
        if (dialogueObject == null) { Debug.LogWarning("[DialogueHUB] dialogueObject is null"); return; }

        dialogueObject.SetActive(true);
        if (miraBox != null) miraBox.SetActive(speaker == "Mira");
        if (kaelBox != null) kaelBox.SetActive(speaker == "Kael");

        if (speaker == "Mira" && miraAnimator != null) miraAnimator.StartEffect(message, effect);
        else if (speaker == "Kael" && kaelAnimator != null) kaelAnimator.StartEffect(message, effect);
    }

    public void CloseDialogue()
    {
        if (dialogueObject != null) dialogueObject.SetActive(false);
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