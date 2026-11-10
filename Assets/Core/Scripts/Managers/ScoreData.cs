using System;

namespace PowerPulse.Core
{
    /// <summary>
    /// Serializable local score record, persisted as JSON by the SaveManager.
    /// </summary>
    [Serializable]
    public class ScoreData
    {
        /// <summary>Best score reached in a single run.</summary>
        public int BestScore;
    }
}
