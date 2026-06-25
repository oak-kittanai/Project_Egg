using System.Collections;
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
    }

    public void SetCharacterType(bool bird)
    {
        isBird = bird;

        if (oxygenContainer != null) oxygenContainer.SetActive(false);
        if (flightBar != null) flightBar.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isTrackingFlight && activeRunner != null)
        {
            float remainingTime = activeFlightTimer.RemainingTime(activeRunner) ?? 0f;
            flightBar.value = remainingTime;
            if (remainingTime <= 0f) StopFlightBar();
        }

        if (isTrackingOxygen && activeRunner != null)
        {
            float remainingTime = activeOxygenTimer.RemainingTime(activeRunner) ?? 0f;
            int currentBubbles = Mathf.CeilToInt(remainingTime);

            UpdateOxygenBubbles(currentBubbles);

            if (remainingTime <= 0f)
            {
                isTrackingOxygen = false;
                UpdateOxygenBubbles(0);
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

        lastBubblesCount = -1;

        UpdateOxygenBubbles(maxBubbles);
    }

    public void StopOxygenTracking()
    {
        isTrackingOxygen = false;
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

    public void ShowStunIcon()
    {
        if (stunIcon != null) stunIcon.SetActive(true);
        if (stunAnimator != null) stunAnimator.Play("Stun");
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
    [SerializeField] private float secondsPerLine = 3f;

    private Coroutine miniDialogueCoroutine;

    public void ShowMiniDialogue(DialogueConfig[] sequence)
    {
        if (miniDialogueContainer == null || sequence == null || sequence.Length == 0) return;

        if (miniDialogueCoroutine != null) StopCoroutine(miniDialogueCoroutine);
        miniDialogueCoroutine = StartCoroutine(PlayMiniDialogueSequence(sequence));
    }

    private IEnumerator PlayMiniDialogueSequence(DialogueConfig[] sequence)
    {
        miniDialogueContainer.SetActive(true);

        foreach (DialogueConfig config in sequence)
        {
            DialogueData data = JsonReader.Read(config.JsonFile);
            if (data?.lines == null) continue;

            foreach (DialogueLine line in data.lines)
            {
                string text = config.isThaiLanguage ? line.thai : line.eng;
                string speakerLabel = string.IsNullOrEmpty(line.speaker) ? "" : $"{line.speaker}: ";

                if (miniTextAnimator != null) miniTextAnimator.StartEffect(speakerLabel + text, config.effect);
                else if (miniDialogueText != null) miniDialogueText.text = speakerLabel + text;

                yield return new WaitForSeconds(secondsPerLine);
            }
        }

        HideMiniDialogue();
        miniDialogueCoroutine = null;
    }

    public void HideMiniDialogue()
    {
        if (miniDialogueCoroutine != null) { StopCoroutine(miniDialogueCoroutine); miniDialogueCoroutine = null; }
        if (miniDialogueContainer != null) miniDialogueContainer.SetActive(false);
    }
}