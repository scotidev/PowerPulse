using System;

namespace PowerPulse.Core
{
    /// <summary>
    /// Contract of the global game state manager: exposes the current state,
    /// state transitions and the time scale control.
    /// </summary>
    public interface IGameManager : IGameService
    {
        /// <summary>The current top-level state of the game.</summary>
        GameState CurrentState { get; }

        /// <summary>Raised after the state changes, with the new state.</summary>
        event Action<GameState> StateChanged;

        /// <summary>Transitions the game into the given state.</summary>
        /// <param name="newState">The state to transition into.</param>
        void SetState(GameState newState);

        /// <summary>Sets <c>Time.timeScale</c> (and <c>fixedDeltaTime</c>) to the given value.</summary>
        /// <param name="scale">1 = normal, 0.2 = tutorial slow motion.</param>
        void SetTimeScale(float scale);

        /// <summary>Freezes the game by setting the time scale to zero.</summary>
        void PauseTime();

        /// <summary>Restores the last non-zero time scale.</summary>
        void ResumeTime();
    }
}
