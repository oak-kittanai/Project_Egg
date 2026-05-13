using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class OffScreenIndicator : NetworkBehaviour
{
    [Header("Ref")]
    public MovementCharacter targetPlayer;
    public RectTransform indicatorUI;
    public RectTransform arrowPivot;

    [Header("UI Components")]
    public Image portraitImage;
    public Image arrowImage;
    public Image profileFrameImage;

    [Header("Bird/Mira Settings")]
    public Sprite birdFaceSprite;
    public Sprite birdArrowSprite;

    [Header("Duck/Kael Settings")]
    public Sprite duckFaceSprite;
    public Sprite duckArrowSprite;

    [Header("Settings")]
    public float edgePadding = 50f;

    private Camera mainCam;

    public override void Spawned()
    {
        targetPlayer = null;

        if (indicatorUI != null)
        {
            indicatorUI.gameObject.SetActive(false);
        }

        Debug.Log("UI Indicator Work fine");
    }

    private void Update()
    {
        if (targetPlayer != null) return;

        FindAndSetFriendTarget();
    }

    private void LateUpdate()
    {
        if (mainCam == null)
        {
            mainCam = CameraCharacter.LocalCamera;
        }

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

    public void SetTarget(MovementCharacter friend, Sprite faceSprite, Sprite arrowSprite)
    {
        targetPlayer = friend;

        if (portraitImage != null && faceSprite != null)
        {
            portraitImage.sprite = faceSprite;
        }

        if (arrowImage != null && arrowSprite != null)
        {
            arrowImage.sprite = arrowSprite;
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
            mainCam = myLocalPlayer.GetComponentInChildren<Camera>();

            if (myFriendPlayer.isBird)
            {
                SetTarget(myFriendPlayer, birdFaceSprite, birdArrowSprite);
            }
            else
            {
                SetTarget(myFriendPlayer, duckFaceSprite, duckArrowSprite);
            }
        }
    }
}