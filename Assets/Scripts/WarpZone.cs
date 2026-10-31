using Fusion;
using System.Threading.Tasks;
using UnityEngine;

public class WarpZone : NetworkBehaviour
{
    [Header("Warp Settings")]
    [SerializeField] string nextSceneBuildString;
    [SerializeField] bool returnToSession = false;

    [Header("Check Player")]
    [Networked] bool playerBird { get; set; }
    [Networked] bool playerDuck { get; set; }

    private bool isWarping = false;
    private const float ClearSeconds = 2.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority || isWarping) return;

        MovementCharacter[] allCharacters = other.GetComponents<MovementCharacter>();

        foreach (var character in allCharacters)
        {
            if (character.enabled)
            {
                if (character.isBird) playerBird = true;
                else playerDuck = true;

                // Solo: ตัวตามหลังไม่มี collider ตัวที่คุมอยู่ถึงก็พอ
                if ((playerBird && playerDuck) || SoloParty.IsActive)
                {
                    isWarping = true;
                    _ = ExecuteWarp();
                }
                break;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!HasStateAuthority || isWarping) return;

        MovementCharacter[] allCharacters = other.GetComponents<MovementCharacter>();

        foreach (var character in allCharacters)
        {
            if (character.enabled)
            {
                if (character.isBird) playerBird = false;
                else playerDuck = false;

                break;
            }
        }
    }

    private async Task ExecuteWarp()
    {
        if (SoloParty.IsActive)
        {
            SoloHud.ShowHint("PROTOTYPE CLEAR", ClearSeconds);
            await Task.Delay(Mathf.RoundToInt(ClearSeconds * 1000f));
            RPC_ReturnToSession();
            return;
        }

        if (returnToSession)
        {
            RPC_ReturnToSession();
            return;
        }

        GameManager.Instance.ResetLoadingStateForNextLevel();
        GameManager.Instance.ShowGlobalLoadingScreen();
        await GameManager.Instance.LoadNextLevel(nextSceneBuildString);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReturnToSession()
    {
        GameManager.Instance.ShowGlobalLoadingScreen();
        GameManager.Instance.BackToSessionScene();
    }
}