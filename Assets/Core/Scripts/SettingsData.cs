using System;

namespace PowerPulse.Core
{
    /// <summary>
    /// Serializable player settings, persisted as JSON by the SaveManager.
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        /// <summary>Master volume, normalized between 0 and 1.</summary>
        public float MasterVolume = 1f;

        /// <summary>Music volume, normalized between 0 and 1.</summary>
        public float MusicVolume = 1f;

        /// <summary>SFX volume, normalized between 0 and 1.</summary>
        public float SfxVolume = 1f;

        /// <summary>Whether the audio output is muted.</summary>
        public bool IsMuted;
    }
}
