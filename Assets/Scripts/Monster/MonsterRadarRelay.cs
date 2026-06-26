using UnityEngine;

public class MonsterRadarRelay : MonoBehaviour
{
    private BaseMonster owner;

    private void Awake()
    {
        owner = GetComponentInParent<BaseMonster>();
    }

    private void OnTriggerEnter2D(Collider2D other) => owner?.NotifyRadarTriggerEnter(other);
    private void OnTriggerExit2D(Collider2D other) => owner?.NotifyRadarTriggerExit(other);
}
