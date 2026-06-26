using System.Collections;
using UnityEngine;

// คุมจังหวะ/ลำดับการเล่น Mini Dialogue แบบรวมศูนย์ — TriggerDialogue แค่ส่ง sequence มาให้เล่น
// ตัวนี้เล่นบทแรกทันที ตั้งเวลาเอง แล้วพอถึงเวลาก็หาตัวละครที่ชื่อตรงกับ Speaker ของบทถัดไปเพื่อส่งให้เล่นต่อ
public class MiniDialogueManager : MonoBehaviour
{
    public static MiniDialogueManager Instance { get; private set; }

    [SerializeField] private float secondsPerLine = 3f;

    private Coroutine playRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayMiniDialogueSequence(DialogueConfig[] sequence)
    {
        if (sequence == null || sequence.Length == 0) return;

        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PlaySequenceRoutine(sequence));
    }

    private IEnumerator PlaySequenceRoutine(DialogueConfig[] sequence)
    {
        foreach (DialogueConfig config in sequence)
        {
            DialogueData data = JsonReader.Read(config.JsonFile);
            if (data?.lines == null) continue;

            MovementCharacter speaker = FindSpeaker(config.NameofSpeaker);
            if (speaker == null) Debug.LogWarning($"MiniDialogueManager: no speaker found for '{config.NameofSpeaker}'");

            foreach (DialogueLine line in data.lines)
            {
                if (speaker != null && speaker.localGUI != null)
                {
                    string text = config.isThaiLanguage ? line.thai : line.eng;
                    speaker.localGUI.ShowMiniDialogueLine(config.NameofSpeaker, text, config.effect);
                }

                yield return new WaitForSeconds(secondsPerLine);

                if (speaker != null && speaker.localGUI != null)
                    speaker.localGUI.HideMiniDialogue();
            }
        }

        playRoutine = null;
    }

    private MovementCharacter FindSpeaker(string speakerName)
    {
        MovementCharacter[] allPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            bool isMatch = (speakerName == "Mira" && p.isBird) || (speakerName == "Kael" && !p.isBird);
            if (isMatch) return p;
        }
        return null;
    }
}
