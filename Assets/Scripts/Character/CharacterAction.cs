using Fusion;
using System;
using UnityEngine;

public class CharacterAction : NetworkBehaviour
{
    InputControl controller;
    CharacterStats stats;


    public SkillSet characterSkillSet;

    public void Setup()
    {
        controller = GetComponent<InputControl>();
        stats = GetComponent<CharacterStats>();
    }

    public void Flying(float stamina, bool onground, float flyspeed, Rigidbody2D rb2D)
    {
        if ( stamina > 0 && !onground)
        {
            Debug.Log("Flyyy");
            Vector2 movement = Vector2.up * flyspeed * Time.deltaTime;
            rb2D.AddForce(movement, ForceMode2D.Impulse);
        }
        else
        {
            Debug.Log("not enough stamina");
        }
    }
}

[Serializable]
public class SkillSet
{
    [Header("Eagle")]
    public float _flyDuration;
    public float _flySpeed;


    [Header("Duck")]
    public bool isFloating;
    public float floatingSpeed;
    // floating animation

    public bool isDive;
    public float diveSpeed;
    // dive animation


    // Duck
    public void WaterFloating()
    {
        // Set speed
        int i = 1;
        if (i == 1) // if on top of water
        {
            float speed;
            float walkSpeed = floatingSpeed;
            // floating animation

            speed = walkSpeed;

            if (i == 2) // if press some button to dive
            {
                speed = diveSpeed;
                // dive animation
            }
        }
    }

    

    // Eagle

}
