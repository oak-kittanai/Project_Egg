using Fusion;
using UnityEngine;
public class CharacterStats : NetworkBehaviour
{
    [Header("Ref")]
    private CharacterAnimation cAnimation;

    [Header("Base Config")]
    public int s_maxHealth = 5;
    public float s_maxStamina = 30f;

    [Header("Movement Config")]
    public float s_walkSpeed = 10f;
    public float maxSpeed = 20f;
    public float s_jumpForce = 12f;
    public float acceleration = 5f;
    public float deceleration = 5f;
    public float s_flySpeed = 8f;

    [Header("Identity")]
    [Networked] public characterType skinType { get; set; }

    private void Awake()
    {
        cAnimation = GetComponent<CharacterAnimation>();
    }

    public override void Spawned()
    {
        if (cAnimation != null)
        {
            cAnimation.UpdateSkin(skinType);
        }
        else Debug.LogError("can't find CharacterAnimation");
    }
}