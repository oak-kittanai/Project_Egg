using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueVoteManager : NetworkBehaviour
{
    public static DialogueVoteManager Instance { get; private set; }

    [Networked] public NetworkBool HostReady { get; set; }
    [Networked] public NetworkBool ClientReady { get; set; }

    private bool isDialogueActive = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!isDialogueActive) return;
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            SubmitReady();
    }

    // เรียกตอนเริ่ม Dialogue sequence ใหม่ (จาก TriggerDialogue)
    public void StartVoteSession()
    {
        isDialogueActive = true;
        if (HasStateAuthority)
        {
            HostReady = false;
            ClientReady = false;
        }
        else
        {
            RPC_ResetVotes();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ResetVotes()
    {
        HostReady = false;
        ClientReady = false;
    }

    // เรียกตอนผู้เล่นอ่านจบ sequence ตามปกติ (จาก DialogueManager) หรือกด Tab
    public void SubmitReady()
    {
        if (!isDialogueActive) return;
        RPC_SubmitReady(Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitReady(PlayerRef playerRef)
    {
        if (playerRef == Runner.LocalPlayer) HostReady = true;
        else ClientReady = true;

        int activePlayers = Runner.ActivePlayers.Count();
        int readyCount = (HostReady ? 1 : 0) + (ClientReady ? 1 : 0);

        if (readyCount >= activePlayers)
            RPC_CloseDialogueAll();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CloseDialogueAll()
    {
        isDialogueActive = false;
        DialogueHUB.Instance?.CloseDialogue();
        AudioManager.Instance?.StopBGM();
        GameManager.Instance?.SetPause_RPC(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
