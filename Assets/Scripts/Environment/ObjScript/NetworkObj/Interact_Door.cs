using Fusion;
using UnityEngine;

public class Interact_Door : NetworkBehaviour
{
    [Header("Movement Settings")]
    [Range(0.0f, 100f)]
    [SerializeField] float speed;
    [Range(0.0f, 100f)]
    [SerializeField] float distance;
    [SerializeField] bool isVertical = true;

    [Networked] public NetworkBool IsOpen { get; set; }

    private Vector3 startPosition;
    private Vector3 endPosition;

    public override void Spawned()
    {
        startPosition = transform.position;

        if (isVertical)
            endPosition = startPosition + new Vector3(0, distance, 0);
        else
            endPosition = startPosition + new Vector3(distance, 0, 0);
    }

    public override void Render()
    {
        Vector3 targetPosition = IsOpen ? endPosition : startPosition;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    public void SetDoorState(bool open)
    {
        if (HasStateAuthority) IsOpen = open;
        else RPC_SetDoorState(open);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetDoorState(NetworkBool open)
    {
        IsOpen = open;
    }
}