using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInterface : MonoBehaviour
{
    public static PlayerInterface Instance;

    [Header("Profile Setting")]
    public Image characterProfile_Ref;
    public Sprite character_Duck;
    public Sprite character_Bird;

    [Header("Health Setting")]
    public Image HealthBar_Ref;
    public Sprite fullHealth;
    public Sprite halfHealth;
    public Sprite lowHealth;
    public Sprite emptyHealth;

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

    private void Start()
    {
        HideQuestUI();
    }

    public void UpdateHealthUI(int currentHp)
    {
        if (HealthBar_Ref == null) return;
        switch (currentHp)
        {
            case 3: HealthBar_Ref.sprite = fullHealth; break;
            case 2: HealthBar_Ref.sprite = halfHealth; break;
            case 1: HealthBar_Ref.sprite = lowHealth; break;
            default: HealthBar_Ref.sprite = emptyHealth; break;
        }
    }

    public void UpdateProfileUI(bool isBird)
    {
        if (characterProfile_Ref != null)
        {
            characterProfile_Ref.sprite = isBird ? character_Bird : character_Duck;
        }
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
    public void ShowInteractPrompt(Transform targetItem)
    {
        currentInteractTarget = targetItem;
        if (interactPromptObj != null && !interactPromptObj.activeSelf)
        {
            interactPromptObj.SetActive(true);
        }
    }

    public void HideInteractPrompt()
    {
        currentInteractTarget = null;
        if (interactPromptObj != null && interactPromptObj.activeSelf)
        {
            interactPromptObj.SetActive(false);
        }
    }
}