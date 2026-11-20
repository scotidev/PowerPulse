using UnityEngine;
using UnityEngine.SceneManagement;

namespace PowerPulse.Core
{
    /// <summary>
    /// Creates the scene-level GameFlow as soon as a scene is loaded, so the
    /// scene files stay clean and the player does not need to add anything in
    /// the Editor. Lives in both the Menu and the Game scene: the menu needs the
    /// flow to start a run, and the Game scene needs it to pause/resume/restart.
    /// </summary>
    public static class SceneFlowBootstrap
    {
        /// <summary>Name of the menu scene, as set in Build Settings.</summary>
        private const string c_MenuSceneName = "Menu";

        /// <summary>Name of the game scene, as set in Build Settings.</summary>
        private const string c_GameSceneName = "Game";

        private static bool s_Hooked;

        /// <summary>
        /// Called automatically by Unity after the first scene loads.
        /// We hook SceneManager.sceneLoaded here so we catch every subsequent load.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (s_Hooked) return;
            s_Hooked = true;

            SceneManager.sceneLoaded += OnSceneLoaded;

            // Also handle the first scene (already loaded when this runs).
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string sceneName = scene.name;

            if (sceneName != c_GameSceneName && sceneName != c_MenuSceneName)
            {
                return;
            }

            // Recreated on every load of a supported scene: the previous one was
            // destroyed with its scene, so a return to the menu and a new run
            // still get a working flow.
            if (Object.FindAnyObjectByType<GameFlow>() != null)
            {
                return;
            }

            GameObject flowObject = new GameObject("GameFlow");
            flowObject.AddComponent<GameFlow>();

            Debug.Log($"[SceneFlowBootstrap] GameFlow created in the '{sceneName}' scene.");
        }
    }
}
