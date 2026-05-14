using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WeighingScale_Mech : NetworkBehaviour
{
    [SerializeField]
    ItemWeightConfigs[] itemConfigs;

    [SerializeField] Collider2D leftPlateCollider;
    [SerializeField] List<Collider2D> leftCheckWeight = new List<Collider2D>();
    [SerializeField] bool isItemOnLeftSide;
    [Networked] float itemOnLeftWeight { get; set; }

    [SerializeField] Collider2D rightPlateCollider;
    [SerializeField] List<Collider2D> rightCheckWeight = new List<Collider2D>();
    [SerializeField] bool isItemOnRightSide;
    [Networked] float itemOnRightWeight { get; set; }

    [SerializeField] string birdName = "Bird", duckName = "Duck";

    [SerializeField] TMP_Text showTextWeight_L;
    [SerializeField] TMP_Text showTextWeight_R;

    [SerializeField] ContactFilter2D checkAbleItems;

    public override void Spawned()
    {
        if (!HasStateAuthority) return;

        foreach (var item in itemConfigs) // for test to check if the item success add the item 
        {
            Debug.Log($"item name :{item.itemName} weight :{item.weight}");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        CheckItemOnScale();
        CalculatorWeight();
        CheckTheFinalWeight();
    }

    public override void Render()
    {
        UpdateHUD();
    }

    public void CalculatorWeight()
    {
        itemOnLeftWeight = 0;
        itemOnRightWeight = 0;

        if (isItemOnLeftSide)
        {
            foreach (var item in leftCheckWeight)
            {
                if (item.name.Contains(duckName))
                {
                    itemOnLeftWeight += 50f;
                }
                else if (item.name.Contains(birdName))
                {
                    itemOnLeftWeight += 30f;
                }

                foreach (var config in itemConfigs)
                {
                    if (item.name.Contains(config.itemName))
                    {
                        itemOnLeftWeight += config.weight;
                        break;
                    }
                }
            }
        }

        if (isItemOnRightSide)
        {
            foreach (var item in rightCheckWeight)
            {
                if (item.name.Contains(duckName))
                {
                    itemOnRightWeight += 50f;
                }
                else if (item.name.Contains(birdName))
                {
                    itemOnRightWeight += 30f;
                }

                foreach (var config in itemConfigs)
                {
                    if (item.name.Contains(config.itemName))
                    {
                        itemOnRightWeight += config.weight;
                        break;
                    }
                }
            }
        }
    }

    public void CheckTheFinalWeight()
    {
        float finalWeight = itemOnLeftWeight + itemOnRightWeight;

        if (finalWeight == 1)
        {
            // Do something
            // Call Function
        }
    }

    public void CheckItemOnScale()
    {
        int leftItemCount = leftPlateCollider.Overlap(checkAbleItems, leftCheckWeight);

        int rightItemCount = rightPlateCollider.Overlap(checkAbleItems, rightCheckWeight);

        isItemOnLeftSide = leftItemCount > 0;

        isItemOnRightSide = rightItemCount > 0;
    }

    public void UpdateHUD()
    {
        showTextWeight_L.text = $"{itemOnLeftWeight}Rm";
        showTextWeight_R.text = $"{itemOnRightWeight}Rm";
    }
}

[System.Serializable]
public class ItemWeightConfigs
{
    public string itemName;
    public float weight;
}