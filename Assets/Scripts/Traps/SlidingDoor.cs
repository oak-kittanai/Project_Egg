using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] Sprite emptyDoor;
    [SerializeField] Sprite orangeStoneDoor;
    [SerializeField] Sprite blueStoneDoor;
    [SerializeField] Sprite fullStoneDoor;

    [Header("DetectBox")]
    public Vector2 boxSize = new Vector2(2f, 1.5f);
    public Vector2 boxOffset = new Vector2(1f, 0f);
    public LayerMask playerLayer;

    [Header("Movement")]
    [SerializeField] float openHeight = 3f;
    [SerializeField] float openSpeed = 2f;

    private SpriteRenderer sr;
    [SerializeField] bool playerInRange;
    [SerializeField] bool isOpening;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = emptyDoor;

        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * openHeight;
    }

    void Update()
    {
        CheckPlayerBox();

        if (playerInRange)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) //Use 1, 2, 3 to open the door pai gone na ja { TT 0 TT }
                SetEmpty();
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                SetOrange();
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                SetBlue();
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                SetFullAndOpen();
        }

        if (isOpening)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                openPosition,
                openSpeed * Time.deltaTime
            );
        }
    }

    void CheckPlayerBox()
    {
        Vector2 boxCenter = (Vector2)transform.position + boxOffset;

        Collider2D hit = Physics2D.OverlapBox(
            boxCenter,
            boxSize,
            0f,
            playerLayer
        );

        playerInRange = hit != null && hit.CompareTag("Player");
    }

    void SetEmpty()
    {
        isOpening = false;
        transform.position = closedPosition;
        sr.sprite = emptyDoor;
    }

    void SetOrange()
    {
        isOpening = false;
        transform.position = closedPosition;
        sr.sprite = orangeStoneDoor;
    }

    void SetBlue()
    {
        isOpening = false;
        transform.position = closedPosition;
        sr.sprite = blueStoneDoor;
    }

    void SetFullAndOpen()
    {
        sr.sprite = fullStoneDoor;
        isOpening = true;
    }

    // ----- Gizmos -----
    void OnDrawGizmosSelected()
    {
        Gizmos.color = playerInRange ? Color.green : Color.red;

        Vector3 center = transform.position + (Vector3)boxOffset;
        Gizmos.DrawWireCube(center, boxSize);
    }
}
