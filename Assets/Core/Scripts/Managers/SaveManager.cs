using System;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Plain C# service that owns the local persistence of the score record and
    /// the player settings. Created with <c>new</c> and registered by the
    /// Bootstraper. Stores the data as JSON in <see cref="PlayerPrefs"/>.
    /// </summary>
    public class SaveManager : ISaveService
    {
        #region STATIC

        /// <summary>PlayerPrefs key of the serialized score record.</summary>
        private const string c_ScoreKey = "PowerPulse.Score";

        /// <summary>PlayerPrefs key of the serialized settings.</summary>
        private const string c_SettingsKey = "PowerPulse.Settings";

        #endregion


        #region FIELDS

        private ScoreData m_ScoreData = new ScoreData();
        private SettingsData m_Settings = new SettingsData();

        #endregion


        #region PUBLIC METHODS

        /// <inheritdoc />
        public void Load()
        {
            LoadScore();
            LoadSettings();
        }

        /// <inheritdoc />
        public void Save()
        {
            SaveScore();
            SaveSettings();
        }

        /// <inheritdoc />
        public ScoreData GetScoreData()
        {
            return m_ScoreData;
        }

        /// <inheritdoc />
        public void SetBestScore(int score)
        {
            if (score <= m_ScoreData.BestScore)
            {
                return;
            }

            m_ScoreData.BestScore = score;
            SaveScore();
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
            SaveSettings();
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>Reads the score record, falling back to defaults when missing or corrupted.</summary>
        private void LoadScore()
        {
            string json = PlayerPrefs.GetString(c_ScoreKey, string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                m_ScoreData = new ScoreData();
                return;
            }

            try
            {
                m_ScoreData = JsonUtility.FromJson<ScoreData>(json) ?? new ScoreData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Corrupted score data, using defaults. {e.Message}");
                m_ScoreData = new ScoreData();
            }
        }

        /// <summary>Reads the settings, falling back to defaults when missing or corrupted.</summary>
        private void LoadSettings()
        {
            string json = PlayerPrefs.GetString(c_SettingsKey, string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                m_Settings = new SettingsData();
                return;
            }

            try
            {
                m_Settings = JsonUtility.FromJson<SettingsData>(json) ?? new SettingsData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Corrupted settings data, using defaults. {e.Message}");
                m_Settings = new SettingsData();
            }
        }

        /// <summary>Writes the score record to PlayerPrefs.</summary>
        private void SaveScore()
        {
            PlayerPrefs.SetString(c_ScoreKey, JsonUtility.ToJson(m_ScoreData));
            PlayerPrefs.Save();
        }

        /// <summary>Writes the settings to PlayerPrefs.</summary>
        private void SaveSettings()
        {
            PlayerPrefs.SetString(c_SettingsKey, JsonUtility.ToJson(m_Settings));
            PlayerPrefs.Save();
        }

        #endregion
    }
}
