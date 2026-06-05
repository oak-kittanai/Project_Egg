using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset;

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }
}