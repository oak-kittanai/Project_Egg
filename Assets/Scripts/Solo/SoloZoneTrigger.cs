using UnityEngine;

// จุดในด่าน Solo: ตัวที่คุมอยู่เดินเข้ามาแล้วปลดสกิล เปิดหน้าสอนเล่น หรือแสดง hint — ทำงานครั้งเดียว
[RequireComponent(typeof(Collider2D))]
public class SoloZoneTrigger : MonoBehaviour
{
    [SerializeField] private MovementCharacter.SkillType[] unlockSkills = new MovementCharacter.SkillType[0];
    [Tooltip("ชื่อใน LevelData.tutorials — ว่าง = ไม่เปิดหน้าสอนเล่น")]
    [SerializeField] private string tutorialName;
    [Tooltip("ข้อความภาษาอังกฤษเท่านั้น ฟอนต์ในเกมไม่มีภาษาไทย")]
    [SerializeField] private string hint;
    [SerializeField] private float hintSeconds = 5f;

    private bool used;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !SoloParty.IsActive) return;
        if (!other.TryGetComponent<MovementCharacter>(out var character) || !character.HasInputAuthority) return;

        used = true;

        foreach (MovementCharacter.SkillType skill in unlockSkills) SoloParty.Instance.UnlockSkill(skill);
        if (!string.IsNullOrEmpty(tutorialName) && LevelData.Instance != null) LevelData.Instance.RequestTutorialShow(tutorialName);
        if (!string.IsNullOrEmpty(hint)) SoloHud.ShowHint(hint, hintSeconds);
    }
}
