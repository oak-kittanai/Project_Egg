using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class ScalePuzzlePhase
{
    public float targetLeftWeight;
    public float targetRightWeight;
    [Tooltip("True = เท่ากัน 2 ฝั่ง (==) / False = (>=)")]
    public bool requireExactMatch = true;
}

public class WeighingScale_Mech : NetworkBehaviour
{
    [Header("Propositions")]
    [Tooltip("ตั้งโจทย์")]
    public ScalePuzzlePhase[] puzzlePhases;
    public S3_SlidingNetworkDoor targetDoor;
    [Tooltip("หน่วงเวลาเปลี่ยนโจทย์")]
    public float delayBetweenPhases = 2f;

    [Header("Visual&Audio")]
    public SpriteRenderer successIndicator; // กล่องเขียวตอนทำโจทย์เสร็จ
    public AudioSource audioSource;
    public AudioClip itemDropSound;
    public AudioClip phaseCompleteSound;

    [Header("Items Config")]
    [SerializeField] ItemWeightConfigs[] itemConfigs;
    [SerializeField] ContactFilter2D checkAbleItems;

    [Header("Left Scale")]
    [SerializeField] Collider2D leftPlateCollider;
    private List<Collider2D> leftCheckWeight = new List<Collider2D>();
    private HashSet<Collider2D> prevLeftItems = new HashSet<Collider2D>();
    [Networked] float itemOnLeftWeight { get; set; }

    [Header("Right Scale")]
    [SerializeField] Collider2D rightPlateCollider;
    private List<Collider2D> rightCheckWeight = new List<Collider2D>();
    private HashSet<Collider2D> prevRightItems = new HashSet<Collider2D>();
    [Networked] float itemOnRightWeight { get; set; }

    [Header("UI")]
    [SerializeField] TMP_Text showTextWeight_L;
    [SerializeField] TMP_Text showTextWeight_R;

    string birdName = "Bird", duckName = "Duck";

    [Networked] public int CurrentPhaseIndex { get; set; }
    [Networked] public NetworkBool IsWaitingNextPhase { get; set; }
    [Networked] private TickTimer PhaseTimer { get; set; }
    [Networked] public NetworkBool IsAllPhasesCompleted { get; set; }

    public override void Spawned()
    {
        if (successIndicator != null) successIndicator.enabled = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || IsAllPhasesCompleted) return;

        if (IsWaitingNextPhase)
        {
            if (PhaseTimer.Expired(Runner))
            {
                IsWaitingNextPhase = false;
                CurrentPhaseIndex++;

                if (CurrentPhaseIndex >= puzzlePhases.Length)
                {
                    IsAllPhasesCompleted = true;
                }
            }
            return;
        }

        CheckItemOnScale();
        CalculatorWeight();
        CheckTheFinalWeight();
    }

    public override void Render()
    {
        UpdateHUD();

        if (successIndicator != null)
        {
            successIndicator.enabled = IsWaitingNextPhase;
        }
    }

    public void CheckItemOnScale()
    {
        leftCheckWeight.Clear();
        rightCheckWeight.Clear();

        leftPlateCollider.Overlap(checkAbleItems, leftCheckWeight);
        rightPlateCollider.Overlap(checkAbleItems, rightCheckWeight);

        bool hasNewItemDropped = false;

        foreach (var item in leftCheckWeight)
        {
            if (!prevLeftItems.Contains(item) && IsValidItemConfig(item))
                hasNewItemDropped = true;
        }

        foreach (var item in rightCheckWeight)
        {
            if (!prevRightItems.Contains(item) && IsValidItemConfig(item))
                hasNewItemDropped = true;
        }

        if (hasNewItemDropped)
        {
            RPC_PlaySound(true); // เสียงวางของ
        }

        prevLeftItems.Clear();
        prevLeftItems.UnionWith(leftCheckWeight);

        prevRightItems.Clear();
        prevRightItems.UnionWith(rightCheckWeight);
    }

    private bool IsValidItemConfig(Collider2D item)
    {
        foreach (var config in itemConfigs)
        {
            if (item.name.Contains(config.itemName)) return true;
        }
        return false;
    }

    public void CalculatorWeight()
    {
        itemOnLeftWeight = CalculateSide(leftCheckWeight);
        itemOnRightWeight = CalculateSide(rightCheckWeight);
    }

    private float CalculateSide(List<Collider2D> items)
    {
        float total = 0;
        foreach (var item in items)
        {
            if (item.name.Contains(duckName)) total += 130f;
            else if (item.name.Contains(birdName)) total += 100f;

            foreach (var config in itemConfigs)
            {
                if (item.name.Contains(config.itemName))
                {
                    total += config.weight;
                    break;
                }
            }
        }
        return total;
    }

    public void CheckTheFinalWeight()
    {
        if (CurrentPhaseIndex >= puzzlePhases.Length) return;

        ScalePuzzlePhase currentPhase = puzzlePhases[CurrentPhaseIndex];

        bool isLeftCorrect = false;
        bool isRightCorrect = false;

        // เช็ควิธีคิดคำตอบ >>> requireExactMatch 
        if (currentPhase.requireExactMatch)
        {
            isLeftCorrect = (itemOnLeftWeight == currentPhase.targetLeftWeight);
            isRightCorrect = (itemOnRightWeight == currentPhase.targetRightWeight);
        }
        else
        {
            isLeftCorrect = (itemOnLeftWeight >= currentPhase.targetLeftWeight);
            isRightCorrect = (itemOnRightWeight >= currentPhase.targetRightWeight);
        }

        if (isLeftCorrect && isRightCorrect)
        {
            IsWaitingNextPhase = true;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, delayBetweenPhases);
            RPC_PlaySound(false);

            // ส่ง Progress
            if (targetDoor != null)
            {
                targetDoor.AdvanceProgress(puzzlePhases.Length);
            }
        }
    }

    public void UpdateHUD()
    {
        if (showTextWeight_L != null) showTextWeight_L.text = $"{itemOnLeftWeight}Rm";
        if (showTextWeight_R != null) showTextWeight_R.text = $"{itemOnRightWeight}Rm";
    }

    // เสียง
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlaySound(bool isDropSound)
    {
        if (audioSource != null)
        {
            AudioClip clipToPlay = isDropSound ? itemDropSound : phaseCompleteSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }
    }
}

[System.Serializable]
public class ItemWeightConfigs
{
    public string itemName;
    public float weight;
}