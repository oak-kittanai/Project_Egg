using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class FadingPlatform : MonoBehaviour
{
    [Header("Fading Settings")]
    [SerializeField] float fadeSpeed = 2.0f;
    [Range(0f, 1f)]
    [SerializeField] float minOpacity = 0.1f;
    public bool isStanding = false;

    private SpriteRenderer sr;

    [SerializeField]  private int playersCount = 0;
    private float targetAlpha = 1.0f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        targetAlpha = (playersCount > 0) ? minOpacity : 1.0f;

        if (!Mathf.Approximately(sr.color.a, targetAlpha))
        {
            Color currentColor = sr.color;
            currentColor.a = Mathf.MoveTowards(currentColor.a, targetAlpha, fadeSpeed * Time.deltaTime);
            sr.color = currentColor;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isStanding = true;

            if (collision.transform.position.y > transform.position.y)
            {
                playersCount++;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isStanding = false;

            playersCount = Mathf.Max(0, playersCount - 1);
        }
    }
}