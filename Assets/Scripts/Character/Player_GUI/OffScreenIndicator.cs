using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class OffScreenIndicator : NetworkBehaviour
{
    [Header("References")]
    public MovementCharacter targetPlayer;
    public RectTransform indicatorUI;
    public RectTransform arrowPivot;

    [Header("UI Images")]
    public Image portraitImage;
    public Sprite birdFaceSprite;
    public Sprite duckFaceSprite;

    [Header("Settings")]
    public float edgePadding = 50f;

    private Camera mainCam;

    public override void Spawned()
    {
        mainCam = Camera.main;
        targetPlayer = null;
        indicatorUI.gameObject.SetActive(false);

        Debug.Log("UI Indicator พร้อมทำงานในระบบ Networked แล้ว");
    }

    private void Update()
    {
        if (targetPlayer != null) return;

        FindAndSetFriendTarget();
    }

    private void LateUpdate()
    {
        if (targetPlayer == null || mainCam == null)
        {
            if (indicatorUI.gameObject.activeSelf) indicatorUI.gameObject.SetActive(false);
            return;
        }

        Vector3 screenPos = mainCam.WorldToScreenPoint(targetPlayer.transform.position);

        bool isOffScreen = screenPos.x <= 0 || screenPos.x >= Screen.width ||
                           screenPos.y <= 0 || screenPos.y >= Screen.height || screenPos.z < 0;

        if (isOffScreen)
        {
            if (!indicatorUI.gameObject.activeSelf) indicatorUI.gameObject.SetActive(true);

            if (screenPos.z < 0)
            {
                screenPos *= -1;
            }

            Vector3 clampedPos = screenPos;
            clampedPos.x = Mathf.Clamp(clampedPos.x, edgePadding, Screen.width - edgePadding);
            clampedPos.y = Mathf.Clamp(clampedPos.y, edgePadding, Screen.height - edgePadding);

            indicatorUI.position = clampedPos;

            Vector3 dir = (targetPlayer.transform.position - mainCam.transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            if (arrowPivot != null)
            {
                arrowPivot.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        else
        {
            if (indicatorUI.gameObject.activeSelf) indicatorUI.gameObject.SetActive(false);
        }
    }

    public void SetTarget(MovementCharacter friend, Sprite faceSprite)
    {
        targetPlayer = friend;

        if (portraitImage != null && faceSprite != null)
        {
            portraitImage.sprite = faceSprite;
        }
    }

    private void FindAndSetFriendTarget()
    {
        MovementCharacter[] allPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);

        if (allPlayers.Length < 2) return;

        MovementCharacter myLocalPlayer = null;
        MovementCharacter myFriendPlayer = null;

        foreach (var player in allPlayers)
        {
            if (player.HasInputAuthority)
            {
                myLocalPlayer = player;
            }
            else
            {
                myFriendPlayer = player;
            }
        }

        if (myLocalPlayer != null && myFriendPlayer != null)
        {
            if (myFriendPlayer.isBird)
            {
                SetTarget(myFriendPlayer, birdFaceSprite);
            }
            else
            {
                SetTarget(myFriendPlayer, duckFaceSprite);
            }
        }
    }
}