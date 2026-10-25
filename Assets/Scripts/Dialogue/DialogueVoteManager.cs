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

    // บอกสถานะโหวตในกล่อง dialogue (FB2-6): ต้องพร้อมครบทุกคนถึงจะปิด แต่เดิมไม่มีอะไรบอกว่ารอใครอยู่
    // อ่านจาก HostReady/ClientReady ที่ sync อยู่แล้ว — ข้อความภาษาอังกฤษตาม dialogue ในเกม (font ไม่มีตัวไทย)
    private const string WaitingForPartnerText = "Waiting for partner... (1/2)";
    private const string PartnerReadyText = "Partner is ready (1/2)";

    public override void Render()
    {
        if (Instance != this || DialogueHUB.Instance == null) return;

        string status = null;
        if (isDialogueActive)
        {
            bool localReady = HasStateAuthority ? HostReady : ClientReady;
            bool partnerReady = HasStateAuthority ? ClientReady : HostReady;

            if (localReady && !partnerReady) status = WaitingForPartnerText;
            else if (partnerReady && !localReady) status = PartnerReadyText;
        }

        DialogueHUB.Instance.SetVoteStatus(status);
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

    // เพลงด่านเล่นต่อหลังปิด dialogue (AU-1) — เดิม StopBGM ตรงนี้ ค้างมาจากสมัย dialogue มีเพลงของตัวเอง
    // (TriggerDialogue ปิดการเล่นเพลง dialogue ไปแล้ว) ทำให้เพลงด่านดับหลัง dialogue แรกทุกด่าน
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CloseDialogueAll()
    {
        isDialogueActive = false;
        DialogueManager.Instance?.CloseDialogue();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
