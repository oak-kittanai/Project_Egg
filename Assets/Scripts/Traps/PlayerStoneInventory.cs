using UnityEngine;

public class PlayerStoneInventory : MonoBehaviour
{
    public StoneType carriedStone = StoneType.None;

    public void RemoveStone()
    {
        carriedStone = StoneType.None;
    }

}
