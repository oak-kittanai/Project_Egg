using Fusion;
using UnityEngine;

public class StoneItem : NetworkBehaviour
{
    [Header("ColorSet")]
    [Tooltip("ติ๊กถูกเป็น Mira ถไม่ติ๊กเป็น Kale")]
    [SerializeField] bool isOrangeStone;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.RPC_RequestAddStone(isOrangeStone);

        GameManager.Instance.RequestDespawn(Object);

        Debug.Log($"Picked up {(isOrangeStone ? "Orange" : "Blue")} Stone!");
    }
}