namespace PowerPulse.Core
{
    /// <summary>
    /// Contract of the local persistence service: loads and stores the score
    /// record and the player settings.
    /// </summary>
    public interface ISaveService : IGameService
    {
        /// <summary>Loads the persisted data from disk into memory.</summary>
        void Load();

        /// <summary>Persists the current in-memory data to disk.</summary>
        void Save();

        /// <summary>Returns the locally persisted score record.</summary>
        /// <returns>The current score record.</returns>
        ScoreData GetScoreData();

        /// <summary>Updates the persisted best score when it is beaten.</summary>
        /// <param name="score">Score achieved at the end of a run.</param>
        void SetBestScore(int score);

        /// <summary>Returns the locally persisted player settings.</summary>
        /// <returns>The current player settings.</returns>
        SettingsData GetSettings();

        /// <summary>Replaces the locally persisted player settings.</summary>
        /// <param name="settings">New settings to persist.</param>
        void SetSettings(SettingsData settings);
    }
}
