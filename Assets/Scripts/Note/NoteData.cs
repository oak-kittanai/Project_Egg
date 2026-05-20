using System;

[Serializable]
public class NoteLocalized
{
    public string thai;
    public string eng;
}

[Serializable]
public class NoteContent
{
    public string NameWhoWrite;
    public NoteLocalized Head;
    public NoteLocalized Desc;
}