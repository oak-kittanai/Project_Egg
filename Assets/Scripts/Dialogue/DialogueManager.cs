using UnityEngine;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private Queue<DialogueLine> lines = new Queue<DialogueLine>();
    private DialogueConfig currentHeader;

    private void Awake() => Instance = this;

    public void StartDialogue(DialogueConfig config)
    {
        currentHeader = config;
        DialogueData data = JsonReader.Read(config.JsonFile);

        lines.Clear();
        foreach (var line in data.lines) lines.Enqueue(line);

        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        if (lines.Count == 0)
        {
            DialogueHUB.Instance.CloseDialogue();
            return;
        }

        DialogueLine line = lines.Dequeue();
        string text = currentHeader.isThaiLanguage ? line.thai : line.eng;

        DialogueHUB.Instance.DisplayLine(currentHeader.NameofSpeaker, text, currentHeader.effect);
    }
}