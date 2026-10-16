using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class PlayerGUI : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] public bool isBird;

    [Header("Oxygen Setting")]
    public GameObject oxygenContainer;
    public Image[] oxygenBubbles;
    public Sprite fullBubbleSprite;
    public Sprite emptyBubbleSprite;

    private TickTimer activeOxygenTimer;
    private bool isTrackingOxygen = false;
    private bool oxygenDepleted = false;
    private int maxBubbles;

    private int lastBubblesCount = -1;

    [Header("Flight Bar")]
    public Slider flightBar;
    private TickTimer activeFlightTimer;
    private NetworkRunner activeRunner;
    private bool isTrackingFlight = false;

    private void Awake()
    {
        if (flightBar != null) flightBar.gameObject.SetActive(false);
        if (stunIcon != null)
        {
            stunRect = stunIcon.transform as RectTransform;
            if (stunRect != null)
            {
                stunBaseX = Mathf.Abs(stunRect.anchoredPosition.x);
                stunBaseY = stunRect.anchoredPosition.y;
            }
            characterSprite = stunIcon.GetComponentInParent<SpriteRenderer>(true);
            characterAnimation = stunIcon.GetComponentInParent<CharacterAnimation>(true);
            stunIcon.SetActive(false);
        }
        if (miniDialogueContainer != null) miniDialogueContainer.SetActive(false);
    }

    public void SetCharacterType(bool bird)
    {
        isBird = bird;

        if (oxygenContainer != null) oxygenContainer.SetActive(false);
        if (flightBar != null) flightBar.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (stunIcon != null && stunIcon.activeSelf) AlignStunIcon();

        if (isTrackingFlight && activeRunner != null)
        {
            float remainingTime = activeFlightTimer.RemainingTime(activeRunner) ?? 0f;
            flightBar.value = remainingTime;
            if (remainingTime <= 0f) StopFlightBar();
        }

        if (isTrackingOxygen && activeRunner != null)
        {
            if (oxygenDepleted)
            {
                UpdateOxygenBubbles(0);
            }
            else
            {
                float remainingTime = activeOxygenTimer.RemainingTime(activeRunner) ?? 0f;
                int currentBubbles = Mathf.CeilToInt(remainingTime);

                UpdateOxygenBubbles(currentBubbles);

                if (remainingTime <= 0f)
                {
                    oxygenDepleted = true;
                    UpdateOxygenBubbles(0);
                }
            }
        }
    }

    public void StartOxygenTracking(TickTimer timer, NetworkRunner runner, int maxAir)
    {
        if (oxygenContainer != null) oxygenContainer.SetActive(true);

        activeOxygenTimer = timer;
        activeRunner = runner;
        maxBubbles = maxAir;
        isTrackingOxygen = true;
        oxygenDepleted = false;

        lastBubblesCount = -1;

        // แสดงช่องฟองเท่าจำนวน oxygen สูงสุดของตัวละคร (นก 6, เป็ด 8) ไม่ให้มีช่องว่างเกินมาตั้งแต่ต้น
        if (oxygenBubbles != null)
        {
            for (int i = 0; i < oxygenBubbles.Length; i++)
            {
                if (oxygenBubbles[i] != null) oxygenBubbles[i].gameObject.SetActive(i < maxBubbles);
            }
        }

        UpdateOxygenBubbles(maxBubbles);
    }

    public void StopOxygenTracking()
    {
        isTrackingOxygen = false;
        oxygenDepleted = false;
        if (oxygenContainer != null) oxygenContainer.SetActive(false);
    }

    public void UpdateOxygenBubbles(int currentBubbles)
    {
        if (oxygenBubbles == null || oxygenBubbles.Length == 0) return;

        if (currentBubbles == lastBubblesCount) return;

        lastBubblesCount = currentBubbles;

        for (int i = 0; i < oxygenBubbles.Length; i++)
        {
            if (i < currentBubbles) oxygenBubbles[i].sprite = fullBubbleSprite;
            else oxygenBubbles[i].sprite = emptyBubbleSprite;
        }
    }

    public void StartFlightBar(TickTimer timer, NetworkRunner runner, float maxFlightTime)
    {
        if (flightBar == null) return;
        activeFlightTimer = timer;
        activeRunner = runner;
        flightBar.maxValue = maxFlightTime;
        flightBar.value = maxFlightTime;
        flightBar.gameObject.SetActive(true);
        isTrackingFlight = true;
    }

    public void StopFlightBar()
    {
        isTrackingFlight = false;
        if (flightBar != null) flightBar.gameObject.SetActive(false);
    }

    [Header("Stun Icon")]
    public GameObject stunIcon;
    public Animator stunAnimator;

    [Tooltip("ตำแหน่งไอคอนมึนตอนท่าว่าย (หน่วยของ Character_Canvas) วัดตอน flipX = false ซึ่งหัวอยู่ขวา")]
    [SerializeField] private Vector2 stunIconSwimOffset = new Vector2(28f, 35f);

    private RectTransform stunRect;
    private SpriteRenderer characterSprite;
    private CharacterAnimation characterAnimation;
    private float stunBaseX;
    private float stunBaseY;

    public void ShowStunIcon()
    {
        if (stunIcon != null) stunIcon.SetActive(true);
        if (stunAnimator != null) stunAnimator.Play("Stun");
        AlignStunIcon();
    }

    // ไอคอนอยู่เหนือหัว: ใน prefab วางไว้ตอนหันซ้าย (flipX = false) — sprite มี pivot กลาง
    // หันขวาหัวจึงย้ายไปอีกฝั่งเท่ากัน -> กลับค่า x ตามทิศหัน (FB2-5 เดิมไอคอนค้างฝั่งซ้ายเสมอ)
    // ท่าว่ายวาดหันกลับด้านและหัวต่ำกว่า จึงใช้ตำแหน่งแยก (FB2-8 — วัดจากภาพเรนเดอร์ prefab)
    private void AlignStunIcon()
    {
        if (stunRect == null) return;

        bool flip = characterSprite != null && characterSprite.flipX;
        Vector2 target;
        if (characterAnimation != null && characterAnimation.IsSwimPose)
            target = new Vector2(flip ? -stunIconSwimOffset.x : stunIconSwimOffset.x, stunIconSwimOffset.y);
        else
            target = new Vector2(flip ? stunBaseX : -stunBaseX, stunBaseY);

        if (stunRect.anchoredPosition != target) stunRect.anchoredPosition = target;
    }

    public void HideStunIcon()
    {
        if (stunIcon != null) stunIcon.SetActive(false);
    }

    [Header("Mini Dialogue")]
    public GameObject miniDialogueContainer;
    public Image miniDialogueBg;
    public TMP_Text miniDialogueText;
    public CustomTextGen miniTextAnimator;

    // แสดงแค่บรรทัดเดียว — จังหวะ/ลำดับสลับ speaker คุมจาก TriggerDialogue (เพราะต้องสลับโชว์คนละตัวละคร)
    public void ShowMiniDialogueLine(string speaker, string text, TextEffectType effect)
    {
        if (miniDialogueContainer == null) return;

        miniDialogueContainer.SetActive(true);
        string speakerLabel = string.IsNullOrEmpty(speaker) ? "" : $"{speaker}: ";

        if (miniTextAnimator != null) miniTextAnimator.StartEffect(speakerLabel + text, effect);
        else if (miniDialogueText != null) miniDialogueText.text = speakerLabel + text;
    }

    public void HideMiniDialogue()
    {
        if (miniDialogueContainer != null) miniDialogueContainer.SetActive(false);
    }
}