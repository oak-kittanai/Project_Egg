using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Trigger Setup")]
    public string targetTutorialName;

    public bool showOnlyOnce = true;
    private bool hasShown = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (showOnlyOnce && hasShown) return;

        if (other.CompareTag("Player"))
        {
            var mc = other.GetComponent<MovementCharacter>();
            if (mc != null && mc.HasInputAuthority)
            {
                if (LevelData.Instance != null)
                {
                    LevelData.Instance.RequestTutorialShow(targetTutorialName);
                    hasShown = true;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var mc = other.GetComponent<MovementCharacter>();
            if (mc != null && mc.HasInputAuthority)
            {
                if (LevelData.Instance != null)
                {
                    LevelData.Instance.RequestTutorialHide();
                }
            }
        }
    }
}