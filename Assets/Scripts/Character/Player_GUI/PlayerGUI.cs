using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class PlayerGUI : MonoBehaviour
{
    public static PlayerGUI Instance;

    [Header("Setting")]
    [SerializeField] public bool isDuck;

    [Header("Oxygen Setting")]
    public GameObject oxygenContainer;
    public Image[] oxygenBubbles;
    public Sprite fullBubbleSprite;
    public Sprite emptyBubbleSprite;

    private TickTimer activeOxygenTimer;
    private bool isTrackingOxygen = false;
    private int maxBubbles;

    [Header("Flight Bar")]
    public Slider flightBar;
    private TickTimer activeFlightTimer;
    private NetworkRunner activeRunner;
    private bool isTrackingFlight = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (flightBar != null) flightBar.gameObject.SetActive(false);
    }

    public void SetCharacterType(bool duck)
    {
        isDuck = duck;

        if (isDuck)
        {
            if (oxygenContainer != null) oxygenContainer.SetActive(true);
            if (flightBar != null) flightBar.gameObject.SetActive(false);
        }
        else
        {
            if (oxygenContainer != null) oxygenContainer.SetActive(false);
            if (flightBar != null) flightBar.gameObject.SetActive(false);
        }
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
        activeOxygenTimer = timer;
        activeRunner = runner;
        maxBubbles = maxAir;
        isTrackingOxygen = true;
    }

    public void StopOxygenTracking()
    {
        isTrackingOxygen = false;
        UpdateOxygenBubbles(maxBubbles);
    }

    public void UpdateOxygenBubbles(int currentBubbles)
    {
        if (oxygenBubbles == null || oxygenBubbles.Length == 0) return;

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
}