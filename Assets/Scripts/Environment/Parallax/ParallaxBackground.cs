using System.Collections.Generic;
using UnityEngine;

// BG-1: คุม ParallaxLayer ทุกชั้นที่อยู่ใต้ตัวเอง (ทุกระดับ)
// - ฟังกล้อง local ผ่าน event แบบ static ของ CameraCharacter → ด่านใหม่ (S1 → S2) ผูกเองตอน OnEnable ไม่ต้องรอกล้องเรียก
// - ตำแหน่งชั้นคิดแบบ absolute จาก cameraReference ไม่สะสม delta → กล้องบินข้ามด่าน/วาร์ปแล้วชั้นไม่เพี้ยนถาวร
// - เป็นภาพของเครื่องตัวเองล้วน ไม่มี network
public class ParallaxBackground : MonoBehaviour
{
    [Tooltip("ตำแหน่งกล้อง (x, y) ที่ชั้นทุกชั้นวางไว้ใน Scene view ตรงพอดี — ปกติคือจุดเกิด + offset ของกล้อง")]
    public Vector2 cameraReference;

    private readonly List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();

    private void Awake()
    {
        SetLayers();
    }

    private void OnEnable()
    {
        CameraCharacter.LocalCameraMoved += Move;
        if (CameraCharacter.LocalCamera != null)
            Move(CameraCharacter.LocalCamera.transform.position);
    }

    private void OnDisable()
    {
        CameraCharacter.LocalCameraMoved -= Move;
    }

    private void SetLayers()
    {
        parallaxLayers.Clear();
        GetComponentsInChildren(true, parallaxLayers);
    }

    private void Move(Vector2 cameraPosition)
    {
        Vector2 offset = cameraPosition - cameraReference;
        for (int i = 0; i < parallaxLayers.Count; i++)
        {
            if (parallaxLayers[i] != null)
                parallaxLayers[i].Apply(offset);
        }
    }
}
