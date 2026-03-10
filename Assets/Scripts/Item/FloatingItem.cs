using Fusion;
using UnityEngine;

public class FloatingItem : NetworkBehaviour
{
    [Header("Floating Settings")]
    [Range(0f, 100f)]
    [SerializeField] float heightDis = 0.5f;
    [Range(0f, 100f)]
    [SerializeField] float velocity = 1f;

    [Header("Rotation Settings")]
    [SerializeField] bool isRotate = true;
    [SerializeField] Vector3 rotationSpeed = new Vector3(0, 0, 50);

    private Vector3 _startPosition;

    public override void Spawned()
    {
        _startPosition = transform.position;
    }

    public override void Render()
    {
        float time = Runner.LocalRenderTime;

        float newY = _startPosition.y + Mathf.Sin(time * Mathf.PI * velocity) * heightDis;
        transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);

        if (isRotate)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}