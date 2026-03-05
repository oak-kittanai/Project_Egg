using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    [Header("Floating Settings")]
    [Range(0f, 100f)]
    [SerializeField] float heightDis = 0.5f; // heightFloating
    [Range(0f, 100f)]
    [SerializeField] float velocity = 1f;   // velocityFloating

    [Header("Rotation Settings")]
    [SerializeField] bool isRotate = true; // The หมุน
    [SerializeField] Vector3 rotationSpeed = new Vector3(0, 50, 0);

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        //คำนวนแบบลิงงง
        float newY = startPosition.y + Mathf.Sin(Time.time * Mathf.PI * velocity) * heightDis;

        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        //หมุนตามแกนที่ตั้ง
        if (isRotate)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}