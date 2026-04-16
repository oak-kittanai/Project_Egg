using UnityEngine;
using System.Collections;

public class SceneStateInitializer : MonoBehaviour
{
    [Header("Target State For This Scene")]
    public SessionState targetState = SessionState.MainMenu;

    private IEnumerator Start()
    {
        yield return null;

        if (SessionManager.Instance != null)
        {
            Debug.Log($"Success Load into Scene State : {targetState}");
            SessionManager.Instance.ChangeState(targetState);
        }
        else
        {
            Debug.LogError("can't find SessionManager ");
        }
    }
}