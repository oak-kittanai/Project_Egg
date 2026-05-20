using Fusion;
using UnityEngine;

public class Note : NetworkBehaviour, Interactable
{
    [Header("Note Data")]
    [SerializeField] private TextAsset noteJson;

    [Header("Language")]
    [SerializeField] private bool useThai = true;

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

    public void Interact(MovementCharacter player)
    {
        if (!player.HasInputAuthority) return;
        if (_noteContent == null) return;

        PlayerInterface.Instance?.ShowNote(_noteContent, useThai);
    }
}
