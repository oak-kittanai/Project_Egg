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
    public SpriteRenderer successIndicator;
    public AudioSource audioSource;
    public AudioClip itemDropSound;
    public AudioClip phaseCompleteSound;

    [Header("Scale Movement")]
    [SerializeField] Transform leftPlateVisual;
    [SerializeField] Transform rightPlateVisual;

    [Header("Rope Visual")]
    [SerializeField] Transform leftRopeVisual;
    [SerializeField] Transform rightRopeVisual;

    private Vector3 leftRopeStartScale;
    private Vector3 rightRopeStartScale;
    private float leftOriginalDist;
    private float rightOriginalDist;

    [Tooltip("น้ำหนักสูงสุด")]
    [SerializeField] float maxWeightToSink = 150f;
    [Tooltip("ระยะลึกแกน Y")]
    [SerializeField] float maxSinkDistance = 1.5f;
    [SerializeField] float plateMoveSpeed = 5f;

    private Vector3 leftPlateStartPos;
    private Vector3 rightPlateStartPos;

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

    [Networked] public int CurrentPhaseIndex { get; set; }
    [Networked] public NetworkBool IsWaitingNextPhase { get; set; }
    [Networked] private TickTimer PhaseTimer { get; set; }
    [Networked] public NetworkBool IsAllPhasesCompleted { get; set; }
    [Networked] private TickTimer SoundCooldownTimer { get; set; }

    public override void Spawned()
    {
        if (successIndicator != null) successIndicator.enabled = false;

        if (leftPlateVisual != null) leftPlateStartPos = leftPlateVisual.position;

        if (rightPlateVisual != null) rightPlateStartPos = rightPlateVisual.position;

        if (leftRopeVisual != null && leftPlateVisual != null)
        {
            leftRopeStartScale = leftRopeVisual.localScale;
            leftOriginalDist = Mathf.Max(Mathf.Abs(leftRopeVisual.position.y - leftPlateVisual.position.y), 0.01f);
        }

        if (rightRopeVisual != null && rightPlateVisual != null)
        {
            rightRopeStartScale = rightRopeVisual.localScale;
            rightOriginalDist = Mathf.Max(Mathf.Abs(rightRopeVisual.position.y - rightPlateVisual.position.y), 0.01f);
        }
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
        UpdatePlateVisuals();

        if (successIndicator != null)
        {
            successIndicator.enabled = IsWaitingNextPhase;
        }
    }

    // --- ขยับตาชั่ง ---
    private void UpdatePlateVisuals()
    {
        if (leftPlateVisual != null)
        {
            float leftSinkRatio = Mathf.Clamp01(itemOnLeftWeight / maxWeightToSink);
            Vector3 leftTargetPos = leftPlateStartPos + new Vector3(0, -leftSinkRatio * maxSinkDistance, 0);
            leftPlateVisual.position = Vector3.Lerp(leftPlateVisual.position, leftTargetPos, Time.deltaTime * plateMoveSpeed);

            if (leftRopeVisual != null)
            {
                float currentDist = Mathf.Abs(leftRopeVisual.position.y - leftPlateVisual.position.y);
                float scaleMultiplier = currentDist / leftOriginalDist;
                leftRopeVisual.localScale = new Vector3(leftRopeStartScale.x, leftRopeStartScale.y * scaleMultiplier, leftRopeStartScale.z);
            }
        }

        if (rightPlateVisual != null)
        {
            float rightSinkRatio = Mathf.Clamp01(itemOnRightWeight / maxWeightToSink);
            Vector3 rightTargetPos = rightPlateStartPos + new Vector3(0, -rightSinkRatio * maxSinkDistance, 0);
            rightPlateVisual.position = Vector3.Lerp(rightPlateVisual.position, rightTargetPos, Time.deltaTime * plateMoveSpeed);

            if (rightRopeVisual != null)
            {
                float currentDist = Mathf.Abs(rightRopeVisual.position.y - rightPlateVisual.position.y);
                float scaleMultiplier = currentDist / rightOriginalDist;
                rightRopeVisual.localScale = new Vector3(rightRopeStartScale.x, rightRopeStartScale.y * scaleMultiplier, rightRopeStartScale.z);
            }
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

        if (hasNewItemDropped && SoundCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            RPC_PlaySound(true);
            SoundCooldownTimer = TickTimer.CreateFromSeconds(Runner, 0.3f);
        }

        prevLeftItems.Clear();
        prevLeftItems.UnionWith(leftCheckWeight);

        prevRightItems.Clear();
        prevRightItems.UnionWith(rightCheckWeight);
    }

    private bool IsValidItemConfig(Collider2D item)
    {
        if (item.GetComponentInParent<Duck_Moveset>() != null) return true;
        if (item.GetComponentInParent<Bird_Moveset>() != null) return true;

        PuzzleItem pItem = item.GetComponentInParent<PuzzleItem>();
        if (pItem != null)
        {
            foreach (var config in itemConfigs)
            {
                if (pItem.ItemName == config.itemName) return true;
            }
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
        HashSet<GameObject> processedObjects = new HashSet<GameObject>();

        foreach (var item in items)
        {
            MovementCharacter player = item.GetComponentInParent<Duck_Moveset>() as MovementCharacter
                                    ?? item.GetComponentInParent<Bird_Moveset>() as MovementCharacter;
            bool foundPlayer = player != null;
            if (foundPlayer && !processedObjects.Contains(player.gameObject))
            {
                processedObjects.Add(player.gameObject);
                if (player is Duck_Moveset) total += 130f;
                else if (player is Bird_Moveset) total += 100f;
            }

            if (foundPlayer) continue;

            PuzzleItem pItem = item.GetComponentInParent<PuzzleItem>();
            if (pItem != null)
            {
                if (processedObjects.Contains(pItem.gameObject)) continue;
                processedObjects.Add(pItem.gameObject);

                foreach (var config in itemConfigs)
                {
                    if (pItem.ItemName == config.itemName)
                    {
                        total += config.weight;
                        break;
                    }
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

            if (targetDoor != null)
            {
                targetDoor.AdvanceProgress(puzzlePhases.Length);
            }
        }
    }

    private float _lastShownLeftWeight = float.NaN;
    private float _lastShownRightWeight = float.NaN;

    public void UpdateHUD()
    {
        if (showTextWeight_L != null && itemOnLeftWeight != _lastShownLeftWeight)
        {
            _lastShownLeftWeight = itemOnLeftWeight;
            showTextWeight_L.text = $"{itemOnLeftWeight}Rm";
        }

        if (showTextWeight_R != null && itemOnRightWeight != _lastShownRightWeight)
        {
            _lastShownRightWeight = itemOnRightWeight;
            showTextWeight_R.text = $"{itemOnRightWeight}Rm";
        }
    }

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