using Fusion;
using UnityEngine;

public class SingletonNetwork<T> : NetworkBehaviour where T : NetworkBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>();

                if (instance == null)
                {
                    Debug.LogError($"SingletonNetwork can't find {typeof(T).Name} in scene");
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);

            OnSetup();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnSetup()
    {
        PlayerSpawn.Instance.Setup();
        CenterHost.instance.GetRunner();
        SessionManager.Instance.Setup();
        SessionHub.Instance.Setup();
    }
}