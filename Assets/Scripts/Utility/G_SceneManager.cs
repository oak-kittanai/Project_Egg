using UnityEngine;

public class G_SceneManager : Singleton<G_SceneManager>
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
