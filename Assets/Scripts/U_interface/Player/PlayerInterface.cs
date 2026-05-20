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

    [Header("Skill Setting")]
    public Transform skillContainer;

    [Header("Skill Prefabs (Insert Here)")]
    public GameObject skillBird_Fly;
    public GameObject skillBird_Throw;
    public GameObject skillDuck_Dive;
    public GameObject skillDuck_Smash;

    [Header("Spawned Skills (Auto-Assigned)")]
    [HideInInspector] public SkillGUI spawnedBirdFly;
    [HideInInspector] public SkillGUI spawnedBirdThrow;
    [HideInInspector] public SkillGUI spawnedDuckDive;
    [HideInInspector] public SkillGUI spawnedDuckSmash;

    // ตัวแปรเก็บสถานะเพื่อป้องกันการ Spawn ซ้ำถ้าไม่ได้เปลี่ยนตัวละคร
    [HideInInspector] public bool isCurrentBirdSetup;
    [HideInInspector] public bool hasSetupSkills = false;

    [Header("Setting")]
    public Button resumeButton;
    public TMP_Text resumePlayerCheckText;

    public Button settingButton;
    public Button quitButton;

    public Button resetButton;
    public TMP_Text resetPlayerCheckText;

    [Header("Note Setting")]
    public GameObject noteObj;
    public TMP_Text noteWriterText;
    public TMP_Text noteHeadText;
    public TMP_Text noteDescText;


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

        TryFindFromScene(persistentRoot);
    }

    private bool TryFindFromRoot(Transform root)
    {
        bool foundAny = false;
        GameObject uiCanvas = GameObject.Find("Canvas");

        // Stats
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

        // Skill
        Transform skillObjT = root.Find("SkillObj");
        if (skillObjT != null)
        {
            Transform skillContainerTransform = skillObjT.Find("SkillContainer");
            if (skillContainerTransform != null)
            {
                skillContainer = skillContainerTransform;
                foundAny = true;
            }
        }

        // Quest
        Transform questDialog = root.Find("QuestDialog_Obj");
        if (questDialog != null)
        {
            Transform questObj = questDialog.Find("QuestContainer");
            if (questObj != null)
            {
                questContainer = questObj.gameObject;
                questText = questObj.Find("QuestText")?.GetComponent<TextMeshProUGUI>();
                questProgressBar = questObj.Find("QuestProgressBar")?.GetComponent<Slider>();
                questItemAmountText = questObj.Find("QuestItemAmountText")?.GetComponent<TextMeshProUGUI>();
                questItemIcon = questObj.Find("QuestItemIcon")?.GetComponent<Image>();
                foundAny = true;
            }
        }
        if (uiCanvas != null)
        {
            // Interact Prompt
            Transform promptObj = uiCanvas.transform.Find("InteractPrompt");
            if (promptObj != null) interactPromptObj = promptObj.gameObject;
        }

        // Setting
        Transform settingObj = uiCanvas.transform.Find("PauseMenu");
        if (settingObj != null)
        {
            resumeButton = settingObj.Find("Resume").GetComponent<Button>();
            resumePlayerCheckText = resumeButton.GetComponentInChildren<TMP_Text>();

            settingButton = settingObj.Find("Setting").GetComponent<Button>();
            quitButton = settingObj.Find("Quit").GetComponent<Button>();

            resetButton = settingObj.Find("Reset").GetComponent<Button>();
            resetPlayerCheckText = resetButton.GetComponentInChildren<TMP_Text>();
            foundAny = true;
        }

        // Note
        Transform noteObjT = uiCanvas.transform.Find("NoteObj");
        if (noteObjT != null)
        {
            noteObj = noteObjT.gameObject;
            noteWriterText = noteObjT.Find("WriterText")?.GetComponent<TMP_Text>();
            noteHeadText = noteObjT.Find("HeadText")?.GetComponent<TMP_Text>();
            noteDescText = noteObjT.Find("DescText")?.GetComponent<TMP_Text>();

            foundAny = true;
            noteObj.SetActive(false);
        }


        return foundAny;
    }

    private void TryFindFromScene(Transform root)
    {
        GameObject uiCanvas = GameObject.Find("Canvas");

        if (uiCanvas == null)
        {
            Debug.LogWarning($"[PlayerInterface] can't find Canvas in current scene");
            return;
        }

        // Stats
        Transform charStats = uiCanvas.transform.Find("Character_Stats_Obj");
        if (charStats != null)
        {
            Transform profileRef = charStats.Find("CharacterProfile");
            if (profileRef != null) characterProfile_Ref = profileRef.GetComponent<Image>();

            Transform healthRef = charStats.Find("HealthBar");
            if (healthRef != null) HealthBar_Ref = healthRef.GetComponent<Image>();
        }

        // Skill
        Transform skillObjT = uiCanvas.transform.Find("SkillObj");
        if (skillObjT != null)
        {
            Transform skillContainerTransform = skillObjT.Find("SkillContainer");
            if (skillContainerTransform != null) skillContainer = skillContainerTransform;
        }

        // Quest
        Transform questDialog = uiCanvas.transform.Find("QuestDialog_Obj");
        if (questDialog != null)
        {
            Transform questObj = questDialog.Find("QuestContainer");
            if (questObj != null)
            {
                questContainer = questObj.gameObject;
                questText = questObj.Find("QuestText")?.GetComponent<TextMeshProUGUI>();
                questProgressBar = questObj.Find("QuestProgressBar")?.GetComponent<Slider>();
                questItemAmountText = questObj.Find("QuestItemAmountText")?.GetComponent<TextMeshProUGUI>();
                questItemIcon = questObj.Find("QuestItemIcon")?.GetComponent<Image>();
            }
        }

        // InteractPrompt
        Transform promptObj = uiCanvas.transform.Find("InteractPrompt");
        if (promptObj != null) interactPromptObj = promptObj.gameObject;

        // Setting
        Transform settingObj = uiCanvas.transform.Find("PauseMenu");
        if (settingObj != null)
        {
            resumeButton = settingObj.Find("Resume").GetComponent<Button>();
            resumePlayerCheckText = resumeButton.GetComponentInChildren<TMP_Text>();

            settingButton = settingObj.Find("Setting").GetComponent<Button>();
            quitButton = settingObj.Find("Quit").GetComponent<Button>();

            resetButton = settingObj.Find("Reset").GetComponent<Button>();
            resetPlayerCheckText = resetButton.GetComponentInChildren<TMP_Text>();
        }

        // Note

        Transform noteObjT = uiCanvas.transform.Find("NoteObj");
        if (noteObjT != null)
        {
            noteObj = noteObjT.gameObject;
            noteWriterText = noteObjT.Find("WriterText")?.GetComponent<TMP_Text>();
            noteHeadText = noteObjT.Find("HeadText")?.GetComponent<TMP_Text>();
            noteDescText = noteObjT.Find("DescText")?.GetComponent<TMP_Text>();

            noteObj.SetActive(false);
        }

        Debug.Log($"[PlayerInterface] found UI in Scene Canvas");
    }

    #region Health&Skill

    public void SetupSkills(bool isBird)
    {
        if (skillContainer == null) return;
        if (hasSetupSkills && isCurrentBirdSetup == isBird) return;

        foreach (Transform child in skillContainer) Destroy(child.gameObject);

        if (isBird)
        {
            if (skillBird_Fly != null) spawnedBirdFly = Instantiate(skillBird_Fly, skillContainer).GetComponent<SkillGUI>();
            if (skillBird_Throw != null) spawnedBirdThrow = Instantiate(skillBird_Throw, skillContainer).GetComponent<SkillGUI>();
        }
        else
        {
            if (skillDuck_Dive != null) spawnedDuckDive = Instantiate(skillDuck_Dive, skillContainer).GetComponent<SkillGUI>();
            if (skillDuck_Smash != null) spawnedDuckSmash = Instantiate(skillDuck_Smash, skillContainer).GetComponent<SkillGUI>();
        }

        isCurrentBirdSetup = isBird;
        hasSetupSkills = true;
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

        SetupSkills(isBird);
    }

    #endregion

    #region Quest
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

    #endregion

    #region Note

    public void ShowNote(NoteContent content, bool useThai = true)
    {
        if (noteObj == null) return;

        if (noteWriterText != null) noteWriterText.text = content.NameWhoWrite;
        if (noteHeadText != null) noteHeadText.text = useThai ? content.Head.thai : content.Head.eng;
        if (noteDescText != null) noteDescText.text = useThai ? content.Desc.thai : content.Desc.eng;

        noteObj.SetActive(true);
    }

    public void HideNote()
    {
        if (noteObj != null) noteObj.SetActive(false);
    }


    #endregion

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