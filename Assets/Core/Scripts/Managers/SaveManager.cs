namespace PowerPulse.Core
{
    /// <summary>
    /// Plain C# service that owns the local persistence of the score record and
    /// the player settings. Created with <c>new</c> and registered by the
    /// Bootstraper. The PlayerPrefs/JSON implementation is completed in Phase 14.
    /// </summary>
    public class SaveManager : ISaveService
    {
        #region FIELDS

        private ScoreData m_ScoreData = new ScoreData();
        private SettingsData m_Settings = new SettingsData();

        #endregion


        #region PUBLIC METHODS

        /// <inheritdoc />
        public void Load()
        {
            // TODO (Phase 14): read the JSON from PlayerPrefs and populate the data.
        }

        /// <inheritdoc />
        public void Save()
        {
            // TODO (Phase 14): serialize the data to JSON and write it to PlayerPrefs.
        }

        /// <inheritdoc />
        public ScoreData GetScoreData()
        {
            return m_ScoreData;
        }

        /// <inheritdoc />
        public void SetBestScore(int score)
        {
            if (score > m_ScoreData.BestScore)
            {
                m_ScoreData.BestScore = score;
            }
        }

        /// <inheritdoc />
        public SettingsData GetSettings()
        {
            return m_Settings;
        }

        /// <inheritdoc />
        public void SetSettings(SettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            m_Settings = settings;
        }

        #endregion
    }
}
