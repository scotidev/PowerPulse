using UnityEngine;
using UnityEngine.SceneManagement;

namespace PowerPulse.Core
{
    /// <summary>
    /// Creates the scene-level GameFlow as soon as the Game scene is loaded, so
    /// the scene file stays clean and the player does not need to add anything
    /// in the Editor. Does nothing in any other scene.
    /// </summary>
    public static class SceneFlowBootstrap
    {
        /// <summary>Name of the scene that owns a GameFlow.</summary>
        private const string c_GameSceneName = "Game";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize()
        {
            if (SceneManager.GetActiveScene().name != c_GameSceneName)
            {
                return;
            }

            if (Object.FindObjectOfType<GameFlow>() != null)
            {
                return;
            }

            GameObject flowObject = new GameObject("GameFlow");
            flowObject.AddComponent<GameFlow>();

            Debug.Log("[SceneFlowBootstrap] GameFlow created in the Game scene.");
        }
    }
}
