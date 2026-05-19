using Fusion;
using UnityEngine;

public class S3_SlidingNetworkDoor : NetworkBehaviour
{
    [Networked] public NetworkBool IsOpen { get; set; }

    [Header("Slide Setting")]
    public Vector3 slideOffset = new Vector3(0f, 3f, 0f);
    public float slideSpeed = 3f;
    public SpriteRenderer doorSpriteRenderer;
    public Sprite[] progressSprites;

    [Networked] public int CurrentProgress { get; set; }

    private Vector3 closedPosition;
    private Vector3 openPosition;

    public override void Spawned()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + slideOffset;
        UpdateSprite();
    }

    public void AdvanceProgress(int totalPhases)
    {
        if (HasStateAuthority)
        {
            CurrentProgress++;

            if (CurrentProgress >= totalPhases)
            {
                IsOpen = true;
            }
        }
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

    public override void Render()
    {
        UpdateSprite();
        Vector3 targetPos = IsOpen ? openPosition : closedPosition;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, slideSpeed * Time.deltaTime);
    }

    private void UpdateSprite()
    {
        if (doorSpriteRenderer != null && progressSprites != null && progressSprites.Length > 0)
        {
            int index = Mathf.Clamp(CurrentProgress, 0, progressSprites.Length - 1);
            doorSpriteRenderer.sprite = progressSprites[index];
        }
    }
}