using Fusion;
using UnityEngine;

public enum StoneShape { Triangle, Star, Circle, Square }

public class StoneInventory : NetworkBehaviour
{
    public static StoneInventory Instance;

    [Networked] public int TriangleCount { get; set; }
    [Networked] public int StarCount { get; set; }
    [Networked] public int CircleCount { get; set; }
    [Networked] public int SquareCount { get; set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void AddStone_RPC(StoneShape shape)
    {
        switch (shape)
        {
            case StoneShape.Triangle: TriangleCount++; break;
            case StoneShape.Star: StarCount++; break;
            case StoneShape.Circle: CircleCount++; break;
            case StoneShape.Square: SquareCount++; break;
        }
    }

    public bool HasStone(StoneShape shape)
    {
        switch (shape)
        {
            case StoneShape.Triangle: return TriangleCount > 0;
            case StoneShape.Star: return StarCount > 0;
            case StoneShape.Circle: return CircleCount > 0;
            case StoneShape.Square: return SquareCount > 0;
        }
        return false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void UseStone_RPC(StoneShape shape)
    {
        switch (shape)
        {
            case StoneShape.Triangle: TriangleCount--; break;
            case StoneShape.Star: StarCount--; break;
            case StoneShape.Circle: CircleCount--; break;
            case StoneShape.Square: SquareCount--; break;
        }
    }
}