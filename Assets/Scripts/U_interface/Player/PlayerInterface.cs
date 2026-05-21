using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

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
    // Quest Setting (With Bar)
    public GameObject questContainerWithBar;
    public Image questBckgroundWithBar;
    public TMP_Text questTextWithBar;
    public Slider questProgressBar;
    public TMP_Text questItemAmountText;
    public Image questItemIconWithBar;

    // Quest Setting (No Bar)
    public GameObject questContainerNoBar;
    public Image questBckgroundNoBar;
    public TMP_Text questHeadTextNoBar;
    public TMP_Text questSubTextNoBar;
    public Image questItemIconNoBar;

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

    public static bool _birdFlyUnlocked;
    public static bool _birdThrowUnlocked;
    public static bool _duckDiveUnlocked;
    public static bool _duckSmashUnlocked;

    private bool _lastIsBird;

    private Transform _cachedSkillContainer;

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

    [Header("Setting")]
    public GameObject settingPanelObj;
    public Slider musicSlider;
    public Slider soundSlider;
    public Button settingLeaveButton;

    [Header("Cutscene UI")]
    public Button skipButton;
    public TMP_Text skipPlayerCheckText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null) RegisterCanvas(canvas);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            RegisterCanvas(canvas);

            SyncSkillStateFromNetwork();

            if (hasSetupSkills)
                SetupSkills(isCurrentBirdSetup);
        }
    }

    private void SyncSkillStateFromNetwork()
    {
        if (GameManager.Instance == null) return;

        _birdFlyUnlocked = GameManager.Instance.SkillBirdFlyUnlocked;
        _birdThrowUnlocked = GameManager.Instance.SkillBirdThrowUnlocked;
        _duckDiveUnlocked = GameManager.Instance.SkillDuckDiveUnlocked;
        _duckSmashUnlocked = GameManager.Instance.SkillDuckSmashUnlocked;
    }

    public void RegisterCanvas(GameObject canvas)
    {
        // Stats
        Transform charStats = canvas.transform.Find("Character_Stats_Obj");
        if (charStats != null)
        {
            characterProfile_Ref = charStats.Find("CharacterProfile")?.GetComponent<Image>();
            HealthBar_Ref = charStats.Find("HealthBar")?.GetComponent<Image>();
        }

        // Skill
        Transform skillObjT = canvas.transform.Find("SkillObj");
        if (skillObjT != null)
        {
            skillContainer = skillObjT.Find("SkillContainer");

            if (_cachedSkillContainer == null)
                _cachedSkillContainer = skillContainer;
        }

        // Quest
        Transform questDialog = canvas.transform.Find("QuestDialog_Obj");
        if (questDialog != null)
        {
            Transform questObjBar = questDialog.Find("QuestContainer_Bar");
            if (questObjBar != null)
            {
                questContainerWithBar = questObjBar.gameObject;
                questBckgroundWithBar = questObjBar.Find("QuestBckground")?.GetComponent<Image>();
                questTextWithBar = questObjBar.Find("QuestDescText")?.GetComponent<TMP_Text>();
                questProgressBar = questObjBar.Find("QuestProgressBar")?.GetComponent<Slider>();
                questItemAmountText = questObjBar.Find("QuestItemAmountText")?.GetComponent<TMP_Text>();
                questItemIconWithBar = questObjBar.Find("QuestItemIcon/Item_Icon")?.GetComponent<Image>();
            }

            Transform questObjNoBar = questDialog.Find("QuestContainer_NoBar");
            if (questObjNoBar != null)
            {
                questContainerNoBar = questObjNoBar.gameObject;
                questBckgroundNoBar = questObjNoBar.Find("QuestBckground")?.GetComponent<Image>();
                questHeadTextNoBar = questObjNoBar.Find("QuestHeadText")?.GetComponent<TMP_Text>();
                questSubTextNoBar = questObjNoBar.Find("QuestSubText")?.GetComponent<TMP_Text>();
                questItemIconNoBar = questObjNoBar.Find("QuestItemIcon/Item_Icon")?.GetComponent<Image>();
            }
        }

        // Interact Prompt
        Transform promptObj = canvas.transform.Find("InteractPrompt");
        if (promptObj != null) interactPromptObj = promptObj.gameObject;

        // PauseMenu
        Transform settingObj = canvas.transform.Find("PauseMenu");
        if (settingObj != null)
        {
            resumeButton = settingObj.Find("Resume")?.GetComponent<Button>();
            resumePlayerCheckText = resumeButton?.GetComponentInChildren<TMP_Text>();
            settingButton = settingObj.Find("Setting")?.GetComponent<Button>();
            quitButton = settingObj.Find("Quit")?.GetComponent<Button>();
            resetButton = settingObj.Find("Reset")?.GetComponent<Button>();
            resetPlayerCheckText = resetButton?.GetComponentInChildren<TMP_Text>();
        }

        // Note
        Transform noteObjT = canvas.transform.Find("NoteObj");
        if (noteObjT != null)
        {
            noteObj = noteObjT.gameObject;
            noteWriterText = noteObjT.Find("WriterText")?.GetComponent<TMP_Text>();
            noteHeadText = noteObjT.Find("HeadText")?.GetComponent<TMP_Text>();
            noteDescText = noteObjT.Find("DescText")?.GetComponent<TMP_Text>();
            noteObj.SetActive(false);
        }

        // In-Game Setting
        Transform settingMenuT = canvas.transform.Find("Setting");
        if (settingMenuT != null)
        {
            settingPanelObj = settingMenuT.gameObject;

            musicSlider = settingMenuT.Find("Music_Slider")?.GetComponent<Slider>();
            soundSlider = settingMenuT.Find("Sound_Slider")?.GetComponent<Slider>();
            settingLeaveButton = settingMenuT.Find("Leave_Button")?.GetComponent<Button>();

            settingPanelObj.SetActive(false);
        }

        // Skip Cutscene
        Transform skipButtoN = canvas.transform.Find("SkipButton");

        if (skipButtoN != null)
        {
            skipButton = skipButtoN.GetComponent<Button>();

            skipPlayerCheckText = skipButtoN.Find("SkipText")?.GetComponent<TMP_Text>();
        }

        HideQuestUI();
        Debug.Log("[PlayerInterface] RegisterCanvas success");

        if (MenuController.Instance != null) MenuController.Instance.RefreshButtons();
    }

    #region Health&Skill

    public void SetupSkills(bool isBird)
    {
        Transform container = _cachedSkillContainer ?? skillContainer;
        if (container == null) return;

        foreach (Transform child in container) Destroy(child.gameObject);

        if (isBird)
        {
            if (skillBird_Fly != null)
            {
                spawnedBirdFly = Instantiate(skillBird_Fly, container).GetComponent<SkillGUI>();
                if (_birdFlyUnlocked) spawnedBirdFly.UnlockSkillImmediate();
            }
            if (skillBird_Throw != null)
            {
                spawnedBirdThrow = Instantiate(skillBird_Throw, container).GetComponent<SkillGUI>();
                if (_birdThrowUnlocked) spawnedBirdThrow.UnlockSkillImmediate();
            }
        }
        else
        {
            if (skillDuck_Dive != null)
            {
                spawnedDuckDive = Instantiate(skillDuck_Dive, container).GetComponent<SkillGUI>();
                if (_duckDiveUnlocked) spawnedDuckDive.UnlockSkillImmediate();
            }
            if (skillDuck_Smash != null)
            {
                spawnedDuckSmash = Instantiate(skillDuck_Smash, container).GetComponent<SkillGUI>();
                if (_duckSmashUnlocked) spawnedDuckSmash.UnlockSkillImmediate();
            }
        }

        isCurrentBirdSetup = isBird;
        hasSetupSkills = true;
    }

    public void UnlockBirdFly()
    {
        _birdFlyUnlocked = true;
        spawnedBirdFly?.UnlockSkill();
        GameManager.Instance?.UnlockSkill_BirdFly();
    }

    public void UnlockBirdThrow()
    {
        _birdThrowUnlocked = true;
        spawnedBirdThrow?.UnlockSkill();
        GameManager.Instance?.UnlockSkill_BirdThrow();
    }

    public void UnlockDuckDive()
    {
        _duckDiveUnlocked = true;
        spawnedDuckDive?.UnlockSkill();
        GameManager.Instance?.UnlockSkill_DuckDive();
    }

    public void UnlockDuckSmash()
    {
        _duckSmashUnlocked = true;
        spawnedDuckSmash?.UnlockSkill();
        GameManager.Instance?.UnlockSkill_DuckSmash();
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
    public void UpdateQuestUI(string detail, int currentProgress, int maxProgress, bool isBar, Sprite icon)
    {
        HideQuestUI();

        if (isBar)
        {
            if (questContainerWithBar != null)
            {
                questContainerWithBar.SetActive(true);
                AnimateQuestUI(questContainerWithBar);
            }

            if (questTextWithBar != null) questTextWithBar.text = detail;
            if (questItemAmountText != null) questItemAmountText.text = $"{currentProgress}/{maxProgress}";
            if (questProgressBar != null)
            {
                questProgressBar.maxValue = maxProgress;
                questProgressBar.value = currentProgress;
            }
            if (questItemIconWithBar != null && icon != null)
            {
                questItemIconWithBar.sprite = icon;
            }
        }
        else
        {
            if (questContainerNoBar != null)
            {
                questContainerNoBar.SetActive(true);
                AnimateQuestUI(questContainerNoBar);
            }

            if (questSubTextNoBar != null) questSubTextNoBar.text = detail;

            if (questItemIconNoBar != null && icon != null)
            {
                questItemIconNoBar.sprite = icon;
            }
        }
    }

    private void AnimateQuestUI(GameObject container)
    {
        if (container == null) return;

        container.transform.DOKill(true);

        container.transform.DOLocalMoveX(-570f, 0f)
            .From()
            .SetEase(Ease.InOutQuint);
    }

    public void HideQuestUI()
    {
        if (questContainerWithBar != null) questContainerWithBar.SetActive(false);
        if (questContainerNoBar != null) questContainerNoBar.SetActive(false);
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

    #region cutscene

    public void SetSkipButtonActive(bool isActive)
    {
        if (skipButton != null) skipButton.gameObject.SetActive(isActive);
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