using UnityEngine;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private List<DialogueLine> lines = new List<DialogueLine>();
    private DialogueConfig currentHeader;

    private int currentLineIndex = 0;

    private void Awake() => Instance = this;

    public void StartDialogue(DialogueConfig config)
    {
        currentHeader = config;
        DialogueData data = JsonReader.Read(config.JsonFile);

        lines.Clear();
        if (data != null && data.lines != null)
        {
            lines.AddRange(data.lines);
        }

        currentLineIndex = 0;
        ShowCurrentLine();
    }

    public void NextLine()
    {
        if (currentLineIndex < lines.Count - 1)
        {
            currentLineIndex++;
            ShowCurrentLine();
        }
        else
        {
            DialogueHUB.Instance.CloseDialogue();
        }
    }

    public void PreviousLine()
    {
        if (currentLineIndex > 0)
        {
            currentLineIndex--;
            ShowCurrentLine();
        }
    }

    private void ShowCurrentLine()
    {
        if (lines.Count == 0) return;

        DialogueLine line = lines[currentLineIndex];

        string text = currentHeader.isThaiLanguage ? line.thai : line.eng;

        DialogueHUB.Instance.DisplayLine(currentHeader.NameofSpeaker, text, currentHeader.effect);
    }
}