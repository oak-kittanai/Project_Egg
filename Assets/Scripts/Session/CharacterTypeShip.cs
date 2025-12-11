using Fusion;
public enum characterType
{
    unknow,
    Duck,
    Bird
}

public class CharacterTypeShip : SingletonNetwork<CharacterTypeShip>
{
    [Networked] public characterType currentHost { get; set; }
    [Networked] public characterType currentClient { get; set; }

    public override void Spawned()
    {
        DontDestroyOnLoad(this);
    }

    public void UpdateType(characterType Type, bool isHost)
    {
        if (HasStateAuthority)
        {
            if (isHost)
            {
                currentHost = Type;
            }
            else
            {
                currentClient = Type;
            }
        }
    }
}
