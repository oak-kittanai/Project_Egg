using UnityEngine;

public class JsonReader
{
    public static DialogueData Read(TextAsset jsonFile)
    {
        if (jsonFile == null) return null;
        return JsonUtility.FromJson<DialogueData>(jsonFile.text);
    }
}