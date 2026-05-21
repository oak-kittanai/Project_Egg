using UnityEngine;

public class NoteReader
{
    public static NoteContent Read(TextAsset jsonFile)
    {
        if (jsonFile == null)
        {
            Debug.LogWarning("[NoteReader] TextAsset is null");
            return null;
        }

        string raw = jsonFile.text.Trim();

        int firstBrace = raw.IndexOf('{');
        int firstColon = raw.IndexOf(':', firstBrace);
        int contentStart = raw.IndexOf('{', firstColon);
        int contentEnd = raw.LastIndexOf('}');
        int outerEnd = raw.LastIndexOf('}', contentEnd - 1);

        if (contentStart < 0 || outerEnd < 0)
        {
            Debug.LogWarning("[NoteReader] JSON format not correct");
            return null;
        }

        string inner = raw.Substring(contentStart, outerEnd - contentStart + 1);
        return JsonUtility.FromJson<NoteContent>(inner);
    }
}