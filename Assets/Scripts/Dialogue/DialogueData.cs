using UnityEngine;

public enum TextEffectType
{
    None,
    Typewriter,
    Shake,
    Wave,
    Angry
}

[System.Serializable]
public class DialogueLine
{
    public string thai;
    public string eng;
}

[System.Serializable]
public class DialogueData
{
    public DialogueLine[] lines;
}

[System.Serializable]
public class DialogueConfig
{
    public string NameofSpeaker;
    public bool isThaiLanguage;
    public TextEffectType effect;
    public TextAsset JsonFile;
}