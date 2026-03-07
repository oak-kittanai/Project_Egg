using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class CrumblingPlatform : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float crumbleDelay = 1.0f;
    [SerializeField] float destroyDelay = 0.5f;
    [SerializeField] float respawnTime = 3.0f;

    [Header("Shake Settings")]
    [SerializeField] bool shakeBeforeFall = true;
    [SerializeField] float shakeAmount = 0.05f;

    private Animator anim;
    private BoxCollider2D boxCollider;
    private SpriteRenderer sr;
    private bool isActivated = false;
    private Vector3 initialPosition;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        initialPosition = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isActivated)
        {
            if (collision.transform.position.y > transform.position.y)
            {
                StartCoroutine(CrumbleSequence());
            }
        }
    }

    IEnumerator CrumbleSequence()
    {
        isActivated = true;
        float timer = 0f;

        while (timer < crumbleDelay)
        {
            timer += Time.deltaTime;
            if (shakeBeforeFall)
            {
                float x = Random.Range(-1f, 1f) * shakeAmount;
                float y = Random.Range(-1f, 1f) * shakeAmount;
                transform.position = initialPosition + new Vector3(x, y, 0);
            }
            yield return null;
        }
        transform.position = initialPosition;
        anim.SetTrigger("Break");
        boxCollider.enabled = false;
        yield return new WaitForSeconds(destroyDelay);
        sr.enabled = false;
        yield return new WaitForSeconds(respawnTime);

        ResetPlatform();
    }

    void ResetPlatform()
    {
        sr.enabled = true;
        boxCollider.enabled = true;

        isActivated = false;
        transform.position = initialPosition;
        anim.SetTrigger("Reset");
    }
}