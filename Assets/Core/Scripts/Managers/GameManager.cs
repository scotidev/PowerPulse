using System;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Plain C# service that owns the global game state machine and the time scale.
    /// Created with <c>new</c> and registered by the Bootstraper in the ServiceLocator.
    /// </summary>
    public class GameManager : IGameManager
    {
        #region STATIC

        private const float c_DefaultTimeScale = 1f;

        #endregion


        #region FIELDS

        private float m_LastTimeScale = c_DefaultTimeScale;

        #endregion


        #region PROPERTIES

        /// <inheritdoc />
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        #endregion


        #region EVENTS

        /// <inheritdoc />
        public event Action<GameState> StateChanged;

        #endregion


        #region PUBLIC METHODS

        /// <inheritdoc />
        public void SetState(GameState newState)
        {
            if (CurrentState == newState)
                return;

            CurrentState = newState;
            StateChanged?.Invoke(newState);
        }

        /// <inheritdoc />
        public void SetTimeScale(float scale)
        {
            if (scale <= 0f)
                return;

            m_LastTimeScale = scale;
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * scale;
        }

        /// <inheritdoc />
        public void PauseTime()
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
        }

        /// <inheritdoc />
        public void ResumeTime()
        {
            SetTimeScale(m_LastTimeScale);
        }

        #endregion
    }
}
