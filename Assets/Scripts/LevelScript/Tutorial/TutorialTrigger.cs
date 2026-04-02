using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    public enum TutorialTarget { Both, Bird, Duck }

    [Header("Trigger Setup")]
    public string targetTutorialName;
    public TutorialTarget showFor = TutorialTarget.Both;
    public bool hideOnExit = false;
    public bool showOnlyOnce = true;

    private bool hasShown = false;

    private bool isPlayerCurrentlyInside = false;

    private Collider2D triggerCollider;
    private ContactFilter2D contactFilter;
    private List<Collider2D> overlappedColliders = new List<Collider2D>();

    private void Start()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();
    }

    private void Update()
    {
        if (showOnlyOnce && hasShown && !isPlayerCurrentlyInside) return;

        int colliderCount = triggerCollider.OverlapCollider(contactFilter, overlappedColliders);

        bool foundLocalPlayer = false;
        MovementCharacter localPlayerMC = null;

        for (int i = 0; i < colliderCount; i++)
        {
            Collider2D other = overlappedColliders[i];
            if (other.CompareTag("Player"))
            {

                var allMCs = other.GetComponents<MovementCharacter>();

                foreach (var mc in allMCs)
                {
                    if (mc != null && mc.enabled && mc.HasInputAuthority)
                    {
                        foundLocalPlayer = true;
                        localPlayerMC = mc;
                        break;
                    }
                }
            }

            if (foundLocalPlayer) break;
        }

        if (foundLocalPlayer && !isPlayerCurrentlyInside)
        {
            isPlayerCurrentlyInside = true;
            HandlePlayerEnter(localPlayerMC);
        }
        else if (!foundLocalPlayer && isPlayerCurrentlyInside)
        {
            isPlayerCurrentlyInside = false;
            HandlePlayerExit();
        }
    }

    private void HandlePlayerEnter(MovementCharacter mc)
    {
        if (showOnlyOnce && hasShown) return;

        bool isBird = (mc is Bird_Moveset);
        bool isDuck = (mc is Duck_Moveset);

        bool canShow = (showFor == TutorialTarget.Both) ||
                       (showFor == TutorialTarget.Bird && isBird) ||
                       (showFor == TutorialTarget.Duck && isDuck);

        if (canShow && LevelData.Instance != null)
        {
            LevelData.Instance.RequestTutorialShow(targetTutorialName);
            hasShown = true;
        }
    }

    private void HandlePlayerExit()
    {
        if (hideOnExit && LevelData.Instance != null)
        {
            LevelData.Instance.RequestTutorialHide();
        }
    }
}