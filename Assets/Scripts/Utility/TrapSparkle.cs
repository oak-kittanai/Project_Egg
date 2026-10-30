using UnityEngine;

// ดาววิบวับสำหรับแปะกับกับดัก (FX-1): ลาก prefab Trap_Sparkle ไปเป็นลูกของกับดักแล้วใช้ได้เลย
// ตอนเปิดใช้จะปรับพื้นที่เกิดดาวให้เท่ารูปกับดัก และวาดหน้ากับดัก — ParticleLayer อยู่หลัง TrapLayer ถ้าไม่ตั้งดาวจะถูกบัง
// เป็นภาพอย่างเดียว แต่ละเครื่องเล่นเอง ไม่เกี่ยวกับ network
[RequireComponent(typeof(ParticleSystem))]
public class TrapSparkle : MonoBehaviour
{
    [Tooltip("ปิดถ้าอยากตั้ง Shape และ Sorting ของ Particle System เอง")]
    [SerializeField] private bool autoFit = true;

    private void OnEnable()
    {
        if (autoFit && transform.parent != null) FitToParent();
    }

    private void FitToParent()
    {
        SpriteRenderer[] sprites = transform.parent.GetComponentsInChildren<SpriteRenderer>();
        if (sprites.Length == 0) return;

        Bounds bounds = sprites[0].bounds;
        SpriteRenderer front = sprites[0];
        foreach (SpriteRenderer sprite in sprites)
        {
            bounds.Encapsulate(sprite.bounds);
            if (SortingLayer.GetLayerValueFromID(sprite.sortingLayerID) > SortingLayer.GetLayerValueFromID(front.sortingLayerID)
                || (sprite.sortingLayerID == front.sortingLayerID && sprite.sortingOrder > front.sortingOrder))
            {
                front = sprite;
            }
        }

        var particles = GetComponent<ParticleSystem>();
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.position = transform.InverseTransformPoint(bounds.center);
        Vector3 scale = transform.lossyScale;
        shape.scale = new Vector3(bounds.size.x / Mathf.Abs(scale.x), bounds.size.y / Mathf.Abs(scale.y), 1f);

        var particleRenderer = GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingLayerID = front.sortingLayerID;
        particleRenderer.sortingOrder = front.sortingOrder + 1;
    }
}
