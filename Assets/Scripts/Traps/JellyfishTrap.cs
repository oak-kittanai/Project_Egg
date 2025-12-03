using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(LineRenderer))]
public class JellyfishTrap : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float explosionRadius = 2.5f;
    [SerializeField] float chargeTime = 1.5f;// เวลาชาร์จ
    [SerializeField] float respawnTime = 3.0f;// เวลาหายไปก่อนเกิดใหม่
    [SerializeField] int damageAmount = 1;
    [SerializeField] float knockbackForce = 5f;  
    [SerializeField] Vector3 scaleUpSize = new Vector3(1.2f, 1.2f, 1f); // ขนาดตอนพองสุด

    [Header("Visuals")]
    [SerializeField] int circleSegments = 50;
    [SerializeField] float explosionAnimDuration = 0.8f;

    [Header("Layer Mask")]
    [SerializeField] LayerMask targetLayer;

    private Animator anim;
    private SpriteRenderer sr;
    private Collider2D col;
    private LineRenderer lineRenderer;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isActivated = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        lineRenderer = GetComponent<LineRenderer>();

        originalScale = transform.localScale;
        targetScale = Vector3.Scale(originalScale, scaleUpSize);

        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActivated && IsTarget(collision))
        {
            StartCoroutine(TrapSequence());
        }
    }

    IEnumerator TrapSequence()
    {
        isActivated = true;


        anim.SetTrigger("Charge");

        float timer = 0f;
        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            float progress = timer / chargeTime;

            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);

            float currentRadius = Mathf.Lerp(0f, explosionRadius, progress);
            DrawCircle(currentRadius);

            yield return null;
        }

        transform.localScale = targetScale;
        DrawCircle(explosionRadius);

        anim.SetTrigger("Explode");
        ClearCircle();

        try
        {
            CheckDamage();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Damage Error (Trap continues anyway): " + e.Message);
        }

        yield return new WaitForSeconds(explosionAnimDuration);

        anim.enabled = false;
        sr.enabled = false;
        col.enabled = false;

        yield return new WaitForSeconds(respawnTime);

        ResetTrap();
    }

    void CheckDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;

                damageable.TakeDamage(damageAmount, knockbackForce, knockbackDir);
            }
        }
    }

    void ResetTrap()
    {

        transform.localScale = originalScale;

        sr.enabled = true;
        col.enabled = true;
        anim.enabled = true; 

        isActivated = false;
        anim.SetTrigger("Reset");
    }

    bool IsTarget(Collider2D collision)
    {
        return ((1 << collision.gameObject.layer) & targetLayer) != 0;
    }

    void DrawCircle(float radius)
    {
        if (radius <= 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = circleSegments + 1;
        float angle = 0f;

        for (int i = 0; i < circleSegments + 1; i++)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0));

            angle += (360f / circleSegments);
        }
    }

    void ClearCircle()
    {
        lineRenderer.positionCount = 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}