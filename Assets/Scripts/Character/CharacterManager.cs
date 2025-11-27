using Fusion;
using System;
using UnityEngine;

public class CharacterManager : NetworkBehaviour
{
    [Header("Referent")]
    CharacterStats stats;
    CharacterAction action;
    CharacterAnimation cAnimation;
    
    CharacterManager selfManager;
    // Mono

    Transform playerBody;

    [Header("Interect")]
    

    [SerializeField] bool _isGetCarry;
    [SerializeField] GameObject _playerToCarry;
    [SerializeField] bool _isCarry;
    [SerializeField] bool _canCarry;

    

    [SerializeField] Vector2 _duckPosition = new Vector2(-2f, 1); // not use anymore i think
    [SerializeField] Vector2 _eaglePosition = new Vector2(-1.5f, -1.5f);
    [SerializeField] Vector2 _positionToBe;

    public override void Spawned()
    {
        Debug.Log("Player has spawn");

        if (HasInputAuthority)
        {
            //playerCamera.gameObject.SetActive(true);
            playerBody = this.transform;
        }
        else
        {
            /*playerCamera.gameObject.SetActive(false);*/
            stats.enabled = false;
            action.enabled = false;
            selfManager.enabled = false;
        }
    }

    #region Skill

    

    #endregion
}
