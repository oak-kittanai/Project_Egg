using UnityEngine;

public class SceneLoadTransition : Singleton<SceneLoadTransition>
{
    public void OnLoadScene(string sceneName)
    {
        // Simple enter Transition
    }

    public void OnExitSceneLoad()
    {
        // Simple exit Transition
    }

    public void OnEnterLoadingScene(string sceneName)
    {
        bool Onloading = false;

        OnLoadScene(sceneName);

        // success Load
        Onloading = LoadingScene();
    }

    public bool LoadingScene()
    {
        // Load Scene Before Enter
        return true;
    }
}
