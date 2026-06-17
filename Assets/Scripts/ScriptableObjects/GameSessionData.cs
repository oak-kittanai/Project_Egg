using UnityEngine;

[CreateAssetMenu(fileName = "GameSessionData", menuName = "Game/GameSessionData")]
public class GameSessionData : ScriptableObject
{
    public characterType hostType;
    public characterType clientType;
}
