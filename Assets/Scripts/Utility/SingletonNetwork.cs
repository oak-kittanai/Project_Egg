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

                /*if (instance == null)
                {
                    Debug.LogWarning($"Singleton not found : {typeof(T).Name}");
                }*/
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
            VerifyLog.Log($"SingletonNetwork<{typeof(T).Name}> created '{name}' (DontDestroyOnLoad)"); // TEMP verify M-2
        }
        else
        {
            VerifyLog.Log($"SingletonNetwork<{typeof(T).Name}> DUPLICATE '{name}' -> Destroy(gameObject) (existing='{instance.name}')"); // TEMP verify M-2
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}