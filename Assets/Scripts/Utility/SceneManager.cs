using UnityEngine;

public class SceneManager : Singleton<SceneManager>
{


    public void LoadScene(string sceneName)
    {
        SceneLoadTransition.Instance.OnLoadScene(sceneName);
    }

    public void LoadMapAndAssetsScene(string sceneName)
    {
        SceneLoadTransition.Instance.OnEnterLoadingScene(sceneName);
    }

    public void ExitScene()
    {
        SceneLoadTransition.Instance.OnExitSceneLoad();
    }
}
