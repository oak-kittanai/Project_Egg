using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialProgress", menuName = "Game/TutorialProgress")]
public class TutorialProgressSO : ScriptableObject
{
    public List<TutorialData> seenTutorials = new List<TutorialData>();
}
