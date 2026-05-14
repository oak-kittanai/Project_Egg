using UnityEngine;

public class MainMenuParallax : MonoBehaviour
{
    public RectTransform backgroundLayer;

    [Header("Movement Setting")]
    public float moveAmount = 20f;
    public bool reverseMove = true;

    public float tiltAmount = 2f;

    public float smoothSpeed = 5f;

    private Vector2 startPos;
    private Quaternion startRot;

    void Start()
    {
        if (backgroundLayer != null)
        {
            startPos = backgroundLayer.anchoredPosition;
            startRot = backgroundLayer.localRotation;
        }
    }

    void Update()
    {
        if (backgroundLayer == null) return;

        float mouseX = (Input.mousePosition.x / Screen.width) * 2f - 1f;
        float mouseY = (Input.mousePosition.y / Screen.height) * 2f - 1f;

        float directionMultiplier = reverseMove ? -1f : 1f;
        Vector2 targetPos = startPos + new Vector2(mouseX * moveAmount * directionMultiplier, mouseY * moveAmount * directionMultiplier);

        Quaternion targetRot = startRot * Quaternion.Euler(mouseY * tiltAmount, -mouseX * tiltAmount, 0f);

        backgroundLayer.anchoredPosition = Vector2.Lerp(backgroundLayer.anchoredPosition, targetPos, Time.deltaTime * smoothSpeed);
        backgroundLayer.localRotation = Quaternion.Lerp(backgroundLayer.localRotation, targetRot, Time.deltaTime * smoothSpeed);
    }
}