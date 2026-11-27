using UnityEngine;
using PowerPulse.Core;

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

            // --- Load bootstrap config (single hardcoded path) ---
            BootstrapConfig config = Resources.Load<BootstrapConfig>("BootstrapConfig");

            if (config == null)
            {
                Debug.LogError("[Bootstraper] BootstrapConfig not found in Resources. Create one via Create > PowerPulse > Bootstrap Config and place it in any Resources folder.");
            }

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

            // --- Save Manager ---
            SaveManager saveManager = new SaveManager();
            ServiceLocator.Current.Register<ISaveService>(saveManager);
            saveManager.Load();

            // --- Audio Manager ---
            // Needs a MonoBehaviour host for AudioSources.
            GameObject audioObject = new GameObject("Audio Manager");
            AudioManager audioManager = audioObject.AddComponent<AudioManager>();
            Object.DontDestroyOnLoad(audioObject);
            ServiceLocator.Current.Register<IAudioService>(audioManager);
            ApplySettingsToAudio(saveManager, audioManager);

            // --- UI Manager ---
            // Instantiated from a prefab referenced in BootstrapConfig so the panel
            // references are set in the Inspector. The game runs without UI when
            // the prefab is missing.
            if (config != null && config.UIManagerPrefab != null)
            {
                GameObject uiObject = Object.Instantiate(config.UIManagerPrefab);
                uiObject.name = config.UIManagerPrefab.name;
                Object.DontDestroyOnLoad(uiObject);

                UIManager uiManager = uiObject.GetComponent<UIManager>();

                if (uiManager == null)
                {
                    Debug.LogError("[Bootstraper] The UI prefab has no UIManager component. The game will run without UI.");
                }
                else
                {
                    ServiceLocator.Current.Register<IUIManager>(uiManager);
                }
            }
            else
            {
                Debug.LogError("[Bootstraper] UI Manager prefab not set in BootstrapConfig. The game will run without UI.");
            }

            Debug.Log("[Bootstraper] All persistent services registered.");

            // NOTE: Layer 1 (persistent) services are the ONLY ones registered here.
            // Layer 2 systems (RunManager, ScoreManager, ColorManager, SpawnManager,
            // Player, HUD, ObjectPool, obstacles, power-ups, portal, tutorial) live
            // inside the Game scene and are NOT registered in the ServiceLocator.
        }

        /// <summary>Applies the persisted settings to the audio service on boot.</summary>
        private static void ApplySettingsToAudio(SaveManager saveManager, AudioManager audioManager)
        {
            SettingsData settings = saveManager.GetSettings();
            audioManager.SetMasterVolume(settings.MasterVolume);
            audioManager.SetMusicVolume(settings.MusicVolume);
            audioManager.SetSfxVolume(settings.SfxVolume);
            audioManager.SetMuted(settings.IsMuted);
        }

        #endregion
    }
}
