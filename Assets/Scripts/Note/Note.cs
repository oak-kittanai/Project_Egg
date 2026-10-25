using Fusion;
using UnityEngine;

public class Note : NetworkBehaviour, Interactable
{
    [Header("Note Data")]
    [SerializeField] private TextAsset noteJson;

    [Header("Language")]
    [SerializeField] private bool useThai = true;

    [Header("Optional Dialogue Trigger")]
    [SerializeField] private TriggerDialogue dialogueToTriggerAfterRead;

    private NoteContent _noteContent;

    public override void Spawned()
    {
        _noteContent = NoteReader.Read(noteJson);
        if (_noteContent == null)
            Debug.LogWarning($"[Note] {gameObject.name} can't read json");
    }

    public bool CanInteract(MovementCharacter player)
    {
        return true;
    }

    // รันบน host (การกด E ตัดสินที่ host — NR-12): เปิดหน้าโน้ตบนเครื่องของคนอ่าน แล้วเรียก dialogue ต่อครั้งเดียว (NT-1)
    // เดิมเช็ค HasInputAuthority แล้วเปิดบนเครื่องที่รันโค้ด -> ตัวของ client ไม่ผ่านบนเครื่อง host จึงอ่านไม่ได้
    public void Interact(MovementCharacter player)
    {
        if (_noteContent == null) return;

        RPC_ShowNote(player.Object.InputAuthority);

        if (dialogueToTriggerAfterRead != null)
        {
            dialogueToTriggerAfterRead.TriggerFromExternal(player);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowNote(PlayerRef reader)
    {
        if (reader != Runner.LocalPlayer || _noteContent == null) return;
        PlayerInterface.Instance?.ShowNote(_noteContent, useThai);
    }
}