using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    #region Static Fields and Constants

    private static T instance;

    private static readonly object ThreadLock = new object();

    private static bool isInitialized;
    private static bool isApplicationQuitting;

    #endregion

    #region Proprieties

    #region Public Proprieties

    public static T Instance
    {
        get
        {
            if (isApplicationQuitting)
            {
                Debug.LogWarning("Instance '" + typeof(T) + "' already destroyed on application quit." +
                                 " Won't create again - returning null.");
                return null;
            }

            lock (ThreadLock)
            {
                if (instance != null)
                {
                    return instance;
                }

                T[] objects = FindObjectsOfType<T>();

                // Throws a error if there is more than 1 monobehaviour component attached in the scene.
                if (objects.Length > 1)
                {
                    Debug.LogError("Something went really wrong - there should never be more than 1 singleton!" +
                                   " Reopening the scene might fix it.");
                    return instance;
                }

                instance = objects.Length > 0 ? objects[0] : FindObjectOfType<T>();

                if (instance == null)
                {
                    GameObject singleton = new GameObject(typeof(T).Name + " (Singleton)");
                    instance = singleton.AddComponent<T>();
                    DontDestroyOnLoad(singleton);

                    isInitialized = false;

                    Debug.Log("An instance of " + typeof(T) + " is needed in the scene, so '" + singleton +
                              "' was created with DontDestroyOnLoad.");
                }

                if (isInitialized)
                {
                    return instance;
                }

                instance.Initialize();
                isInitialized = true;
                Debug.Log("Finish Init " + typeof(T).ToString());
                return instance;
            }
        }
    }

    #endregion

    #endregion

    #region Methods

    #region Monobehaviour Methods

    /// <summary>
    ///     When Unity quits, it destroys objects in a random order.
    ///     In principle, a Singleton is only destroyed when application quits.
    ///     If any script calls Instance after it have been destroyed,
    ///     it will create a buggy ghost object that will stay on the Editor scene
    ///     even after stopping playing the Application. Really bad!
    ///     So, this was made to be sure we're not creating that buggy ghost object.
    /// </summary>
    private void OnDestroy()
    {
        Debug.LogWarning("(Singleton) OnDestroy");

        instance = null;
        isInitialized = false;
    }

    #endregion

    #region Protected Methods

    protected void Awake()
    {
        if (Instance) { }
    }

    protected abstract void Initialize();

    #endregion

    #region Private Methods

    private void OnApplicationQuit()
    {
        Debug.LogWarning("(Singleton) OnApplicationQuit");

        isApplicationQuitting = true;
        instance = null;
    }

    #endregion

    #endregion
}