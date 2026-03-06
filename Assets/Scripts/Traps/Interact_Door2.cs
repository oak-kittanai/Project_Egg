using Fusion;
using UnityEngine;

public class Interact_Door2 : NetworkBehaviour
{
    [Header("Movement")]
    [Range(0.0f, 100f)]
    [SerializeField] float speed = 5f;
    [Range(0.0f, 100f)]
    [SerializeField] float distance = 3f;
    [SerializeField] bool isVertical = true;

    [SerializeField] int requiredButtons = 2;
    private int activatedButtonsCount = 0;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 targetPosition;

    void Start()
    {
        startPosition = transform.position;

        if (isVertical)
            endPosition = startPosition + new Vector3(0, distance, 0);
        else
            endPosition = startPosition + new Vector3(distance, 0, 0);

        targetPosition = startPosition;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    public void RegistButtonPress()
    {
        activatedButtonsCount++;

        if (activatedButtonsCount >= requiredButtons)
        {
            targetPosition = endPosition;
        }
    }
}