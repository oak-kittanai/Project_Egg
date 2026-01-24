using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    public static ParallaxBackground Instance;

    public CameraCharacter parallaxCamera;
    List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        SetLayers();
    }

    public void SetCamera(CameraCharacter newCamera)
    {
        if (parallaxCamera != null)
        {
            parallaxCamera.onCameraTranslate -= Move;
        }

        parallaxCamera = newCamera;

        if (parallaxCamera != null)
        {
            parallaxCamera.onCameraTranslate += Move;
        }
    }

    void SetLayers()
    {
        parallaxLayers.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            ParallaxLayer layer = transform.GetChild(i).GetComponent<ParallaxLayer>();
            if (layer != null)
            {
                parallaxLayers.Add(layer);
            }
        }
    }

    void Move(float delta)
    {
        foreach (ParallaxLayer layer in parallaxLayers)
        {
            layer.Move(delta);
        }
    }
}