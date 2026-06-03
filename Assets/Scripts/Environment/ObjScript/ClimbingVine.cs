using UnityEngine;
using Fusion;

[RequireComponent(typeof(Collider2D))]
public class ClimbingVine : MonoBehaviour, IClimbable
{
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }
}