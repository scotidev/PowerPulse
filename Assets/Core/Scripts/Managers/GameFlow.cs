using UnityEngine;
using UnityEngine.SceneManagement;

namespace PowerPulse.Core
{
    /// <summary>
    /// Owns the run-level flow: start, pause, resume, restart and go to menu.
    /// Lives in the Game scene because it depends on the scene's RunManager.
    /// The persistent UI resolves it with <see cref="Object.FindObjectOfType"/>.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        #region STATIC

        /// <summary>Name of the menu scene, as set in Build Settings.</summary>
        private const string c_MenuSceneName = "Menu";

        /// <summary>Name of the game scene, as set in Build Settings.</summary>
        private const string c_GameSceneName = "Game";

        #endregion


        #region FIELDS

        private IGameManager m_GameManager;
        private ISceneLoader m_SceneLoader;
        private RunManager m_RunManager;
        private float m_LastTimeScale = 1f;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_GameManager = ServiceLocator.Current != null ? ServiceLocator.Current.Get<IGameManager>() : null;
            m_SceneLoader = ServiceLocator.Current != null ? ServiceLocator.Current.Get<ISceneLoader>() : null;
            m_RunManager = Object.FindObjectOfType<RunManager>();
        }

        private void OnEnable()
        {
            if (m_GameManager != null)
            {
                m_GameManager.StateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (m_GameManager != null)
            {
                m_GameManager.StateChanged -= HandleStateChanged;
            }
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>Starts a new game: loads the Game scene and starts the run.</summary>
        public void StartGame()
        {
            if (m_GameManager == null || m_GameManager.CurrentState != GameState.MainMenu)
            {
                return;
            }

            m_GameManager.SetState(GameState.Playing);
            m_SceneLoader?.LoadScene(c_GameSceneName);
        }

        /// <summary>Pauses the run.</summary>
        public void Pause()
        {
            if (m_GameManager == null || m_GameManager.CurrentState != GameState.Playing)
            {
                return;
            }

            m_LastTimeScale = Time.timeScale;
            m_GameManager.SetState(GameState.Paused);
            m_GameManager.PauseTime();
        }

        /// <summary>Resumes the run.</summary>
        public void Resume()
        {
            if (m_GameManager == null || m_GameManager.CurrentState != GameState.Paused)
            {
                return;
            }

            m_GameManager.ResumeTime();
            m_GameManager.SetState(GameState.Playing);

            if (m_RunManager != null)
            {
                m_RunManager.RestoreRun();
            }
        }

        /// <summary>Restarts the run without reloading the scene.</summary>
        public void Restart()
        {
            if (m_GameManager == null || m_GameManager.CurrentState != GameState.GameOver)
            {
                return;
            }

            m_GameManager.ResumeTime();
            m_GameManager.SetState(GameState.Playing);

            if (m_RunManager != null)
            {
                m_RunManager.StartRun();
            }
        }

        /// <summary>Returns to the main menu.</summary>
        public void GoToMenu()
        {
            if (m_GameManager == null)
            {
                return;
            }

            if (m_GameManager.CurrentState != GameState.Paused && m_GameManager.CurrentState != GameState.GameOver)
            {
                return;
            }

            m_GameManager.ResumeTime();
            m_GameManager.SetState(GameState.MainMenu);
            m_SceneLoader?.LoadScene(c_MenuSceneName);
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// The GameFlow only owns the transitions: who starts or restores the run
        /// is the RunManager, which lives in the Game scene.
        /// </summary>
        /// <param name="state">New global game state.</param>
        private void HandleStateChanged(GameState state)
        {
        }

        #endregion
    }
}
