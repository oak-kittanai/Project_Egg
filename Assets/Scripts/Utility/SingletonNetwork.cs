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
                    Debug.LogWarning($"Singleton not found : {typeof(T).Name}");
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
        }
        else
        {
            Destroy(gameObject);
        }

        if (Instance is PlayerSpawn)
        {
            PlayerSpawn.Instance.Setup();
        }


        if (Instance is CenterHost)
        {
            CenterHost.instance.GetRunner();
        }


        if (Instance is SessionManager)
        {
            SessionManager.Instance.Setup();
        }

        if (Instance is SessionHub)
        {
            SessionHub.Instance.Setup();
        }
    }
}