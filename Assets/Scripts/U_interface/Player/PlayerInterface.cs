using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerInterface : MonoBehaviour
{
    public static PlayerInterface Instance;

    [Header("Profile Setting")]
    public Image characterProfile_Ref;
    public Sprite character_Duck;
    public Sprite character_Bird;

    [Header("Health Setting")]
    public Image HealthBar_Ref;
    public Sprite emptyHealth;
    public Sprite FirstHealth;
    public Sprite SecondHealth;
    public Sprite ThirdHealth;
    public Sprite FourthHealth;
    public Sprite FifthHealth;

    [Header("Quest Setting")]
    public GameObject questContainer;
    public TextMeshProUGUI questText;
    public Slider questProgressBar;
    public TextMeshProUGUI questItemAmountText;
    public Image questItemIcon;

    [Header("Interact Prompt")]
    public GameObject interactPromptObj;
    public Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);
    private Transform currentInteractTarget;

    private void Awake()
    {
        if (Instance == null) Instance = this;
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
        FindAllUIReferences(SceneManager.GetActiveScene());
        HideQuestUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAllUIReferences(scene);
    }

    private void FindAllUIReferences(Scene scene)
    {
        Transform persistentRoot = transform.root;
        bool foundFromPersistent = TryFindFromRoot(persistentRoot);

        if (foundFromPersistent)
        {
            Debug.Log($"[PlayerInterface] found UI in Persistent Canvas");
            return;
        }

        TryFindFromScene();
    }

    private bool TryFindFromRoot(Transform root)
    {
        bool foundAny = false;

        Transform charStats = root.Find("Character_Stats_Obj");
        if (charStats != null)
        {
            Transform profileT = charStats.Find("CharacterProfile");
            if (profileT != null)
            {
                characterProfile_Ref = profileT.GetComponent<Image>();
                foundAny = true;
            }

            Transform healthT = charStats.Find("HealthBar");
            if (healthT != null)
            {
                HealthBar_Ref = healthT.GetComponent<Image>();
                foundAny = true;
            }
        }

        Transform questDialog = root.Find("QuestDialog_Obj");
        if (questDialog != null)
        {
            Transform questObj = questDialog.Find("QuestBckground");
            if (questObj != null)
            {
                questContainer = questObj.gameObject;
                questText = questObj.Find("QuestText")?.GetComponent<TextMeshProUGUI>();
                questProgressBar = questObj.Find("QuestProgressBar")?.GetComponent<Slider>();
                questItemAmountText = questObj.Find("QuestItemAmountText")?.GetComponent<TextMeshProUGUI>();
                questItemIcon = questObj.Find("QuestItemIcon")?.GetComponent<Image>();
                foundAny = true;
            }

            Transform promptT = questDialog.Find("InteractPrompt");
            if (promptT != null)
            {
                interactPromptObj = promptT.gameObject;
                foundAny = true;
            }
        }

        return foundAny;
    }

    private void TryFindFromScene()
    {
        GameObject uiCanvas = GameObject.Find("PlayerInterfaceCanvas");

        if (uiCanvas == null)
        {
            Debug.LogWarning($"[PlayerInterface] can't find Canvas in current scene");
            return;
        }

        Transform profileRef = uiCanvas.transform.Find("CharacterProfile");
        if (profileRef != null) characterProfile_Ref = profileRef.GetComponent<Image>();

        Transform healthRef = uiCanvas.transform.Find("HealthBar");
        if (healthRef != null) HealthBar_Ref = healthRef.GetComponent<Image>();

        Transform questObj = uiCanvas.transform.Find("QuestContainer");
        if (questObj != null)
        {
            questContainer = questObj.gameObject;
            questText = questObj.Find("QuestText")?.GetComponent<TextMeshProUGUI>();
            questProgressBar = questObj.Find("QuestProgressBar")?.GetComponent<Slider>();
            questItemAmountText = questObj.Find("QuestItemAmountText")?.GetComponent<TextMeshProUGUI>();
            questItemIcon = questObj.Find("QuestItemIcon")?.GetComponent<Image>();
        }

        Transform promptObj = uiCanvas.transform.Find("InteractPrompt");
        if (promptObj != null) interactPromptObj = promptObj.gameObject;

        Debug.Log($"[PlayerInterface] found UI in Scene Canvas");
    }

    public void UpdateHealthUI(int currentHp)
    {
        if (HealthBar_Ref == null) return;
        switch (currentHp)
        {
            case 5: HealthBar_Ref.sprite = FifthHealth; break;
            case 4: HealthBar_Ref.sprite = FourthHealth; break;
            case 3: HealthBar_Ref.sprite = ThirdHealth; break;
            case 2: HealthBar_Ref.sprite = SecondHealth; break;
            case 1: HealthBar_Ref.sprite = FirstHealth; break;
            default: HealthBar_Ref.sprite = emptyHealth; break;
        }
    }

    public void UpdateProfileUI(bool isBird)
    {
        if (characterProfile_Ref != null)
            characterProfile_Ref.sprite = isBird ? character_Bird : character_Duck;
    }

    public void UpdateQuestUI(string detail, int currentProgress, int maxProgress)
    {
        if (questContainer != null) questContainer.SetActive(true);
        if (questText != null) questText.text = detail;
        if (questItemAmountText != null) questItemAmountText.text = $"{currentProgress}/{maxProgress}";
        if (questProgressBar != null)
        {
            questProgressBar.maxValue = maxProgress;
            questProgressBar.value = currentProgress;
        }
    }

    public void HideQuestUI()
    {
        if (questContainer != null) questContainer.SetActive(false);
    }

    private void LateUpdate()
    {
        if (currentInteractTarget != null && interactPromptObj != null && interactPromptObj.activeSelf)
        {
            interactPromptObj.transform.position = currentInteractTarget.position + promptOffset;
        }
    }

    public void ShowInteract(Transform targetItem)
    {
        currentInteractTarget = targetItem;
        if (interactPromptObj != null && !interactPromptObj.activeSelf)
            interactPromptObj.SetActive(true);
    }

    public void HideInteract()
    {
        currentInteractTarget = null;
        if (interactPromptObj != null && interactPromptObj.activeSelf)
            interactPromptObj.SetActive(false);
    }
}