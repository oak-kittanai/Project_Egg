using UnityEngine;

interface ThrowAbleItem
{
    bool AlreadyThrow { get; set; }

    void PickupItem_RPC(MovementCharacter player);

    bool PickupItem();
}
