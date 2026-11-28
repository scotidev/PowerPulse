using UnityEngine;
using UnityEngine.SceneManagement;
using PowerPulse.Audio;

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

        // Audio
        private IAudioService m_AudioService;

        // Tracks whether we've already started gameplay music for this run
        // Static so it's shared across all GameFlow instances (Menu scene + Game scene)
        private static bool s_GameplayMusicStarted = false;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_GameManager = ServiceLocator.Current != null ? ServiceLocator.Current.Get<IGameManager>() : null;
            m_SceneLoader = ServiceLocator.Current != null ? ServiceLocator.Current.Get<ISceneLoader>() : null;
            m_RunManager = Object.FindAnyObjectByType<RunManager>();

            m_AudioService = ServiceLocator.Current != null ? ServiceLocator.Current.Get<IAudioService>() : null;
        }

        private void OnEnable()
        {
            if (m_GameManager != null)
            {
                m_GameManager.StateChanged += HandleStateChanged;
                
                // The game can already be in a state (MainMenu on boot, or a
                // reloaded scene in any other state). No StateChanged event is
                // raised for the current state, so we have to apply the correct
                // music once here — otherwise the menu music never starts.
                HandleStateChanged(m_GameManager.CurrentState);
            }
            else
            {
                Debug.LogError($"[GameFlow] GameManager is NULL - cannot subscribe to StateChanged!");
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

            s_GameplayMusicStarted = false; // Reset so new run starts fresh music
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

            s_GameplayMusicStarted = false; // Reset when returning to menu
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
            switch (state)
            {
                case GameState.MainMenu:
                    // Crossfade to menu music when returning to menu
                    m_AudioService?.CrossfadeTo(MusicId.Menu);
                    break;

                case GameState.Playing:
                    // Start gameplay music when run begins - only on first transition to Playing
                    if (!s_GameplayMusicStarted && m_AudioService != null)
                    {
                        MusicId nextTrack = m_AudioService.GetNextGameplayTrack();
                        m_AudioService.CrossfadeTo(nextTrack);
                        s_GameplayMusicStarted = true;
                    }
                    else if (m_AudioService == null)
                    {
                        Debug.LogError($"[GameFlow] State=Playing: AudioService is NULL! Cannot start gameplay music.");
                    }
                    break;

                case GameState.GameOver:
                    // Death sequence is handled by ScoreManager.PlayDeathSequence()
                    // which crossfades to death music and plays SFX
                    break;

                case GameState.Paused:
                    // No music change for pause
                    break;
            }
        }

        #endregion
    }
}
