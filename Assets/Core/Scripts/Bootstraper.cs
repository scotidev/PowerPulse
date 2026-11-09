using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Composition root of the game. Runs before any scene loads and creates
    /// every persistent (Layer 1) service, registering each one in the
    /// ServiceLocator. Uses [RuntimeInitializeOnLoadMethod] so it never depends
    /// on scene order or Awake timing.
    /// </summary>
    public static class Bootstraper
    {
        #region STATIC

        /// <summary>Guards against re-execution on domain reload.</summary>
        private static bool s_Initialized = false;

        #endregion


        #region INITIALIZATION

        /// <summary>
        /// Called automatically by Unity before any scene loads.
        /// Creates all global managers and registers them in the ServiceLocator.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (s_Initialized) return;
            s_Initialized = true;

            // --- Initialize the service locator (creates a clean Current) ---
            ServiceLocator.Initialize();

            // --- Game Manager ---
            GameManager gameManager = new GameManager();
            ServiceLocator.Current.Register<IGameManager>(gameManager);

            // --- Scene Loader ---
            // Needs a MonoBehaviour host for coroutines, so it lives on a
            // persistent GameObject created in code.
            GameObject sceneLoaderObject = new GameObject("Scene Loader");
            SceneLoader sceneLoader = sceneLoaderObject.AddComponent<SceneLoader>();
            Object.DontDestroyOnLoad(sceneLoaderObject);
            ServiceLocator.Current.Register<ISceneLoader>(sceneLoader);

            // --- Save Manager (PENDING) ---
            // Uncomment when SaveManager is implemented.
            // SaveManager saveManager = new SaveManager();
            // ServiceLocator.Current.Register<ISaveService>(saveManager);

            // --- Audio Manager (PENDING) ---
            // Uncomment when AudioManager is implemented.
            // Needs a MonoBehaviour host for AudioSources.
            // GameObject audioObject = new GameObject("Audio Manager");
            // AudioManager audioManager = audioObject.AddComponent<AudioManager>();
            // Object.DontDestroyOnLoad(audioObject);
            // ServiceLocator.Current.Register<IAudioService>(audioManager);

            // NOTE: Layer 1 (persistent) services are the ONLY ones registered here.
            // Layer 2 systems (RunManager, ScoreManager, ColorManager, SpawnManager,
            // Player, HUD, ObjectPool, obstacles, power-ups, portal, tutorial) live
            // inside the Game scene and are NOT registered in the ServiceLocator.

            Debug.Log("[Bootstraper] All persistent services registered.");

            // TEMP TEST — remove after checking the fade
            ServiceLocator.Current.Get<ISceneLoader>().LoadScene("Game");
        }

        #endregion
    }
}
