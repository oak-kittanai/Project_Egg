using Fusion;
using System.Threading.Tasks;
using UnityEngine;

public class WarpZone : NetworkBehaviour
{
    [Header("Warp Settings")]
    [SerializeField] string nextSceneBuildString;

    [Header("Check Player")]
    [Networked] bool playerBird { get; set; }
    [Networked] bool playerDuck { get; set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        MovementCharacter[] allCharacters = other.GetComponents<MovementCharacter>();

        foreach (var character in allCharacters)
        {
            if (character.enabled)
            {
                if (character.isBird) playerBird = true;
                else playerDuck = true;

                Debug.Log($"Warp Status -> Bird: {playerBird} | Duck: {playerDuck}");

                if (playerBird && playerDuck)
                {
                    ExecuteWarp();
                }

                break;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        MovementCharacter[] allCharacters = other.GetComponents<MovementCharacter>();

        foreach (var character in allCharacters)
        {
            if (character.enabled)
            {
                if (character.isBird) playerBird = false;
                else playerDuck = false;

                Debug.Log($"Someone left. Warp Status -> Bird: {playerBird} | Duck: {playerDuck}");
                break;
            }
        }
    }

    private async Task ExecuteWarp()
    {
        GameManager.Instance.ResetLoadingStateForNextLevel();
        GameManager.Instance.ShowGlobalLoadingScreen();

        await GameManager.Instance.LoadNextLevel(nextSceneBuildString);

        Debug.Log($"Host is warping everyone to Scene: {nextSceneBuildString}");
    }
}