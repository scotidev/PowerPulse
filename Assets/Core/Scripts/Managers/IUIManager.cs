namespace PowerPulse.Core
{
    /// <summary>
    /// Contract of the UI service: the single owner of every screen panel and
    /// of the in-run HUD. Registered in the ServiceLocator by the Bootstraper.
    /// </summary>
    public interface IUIManager : IGameService
    {
        /// <summary>Shows one panel and hides the others.</summary>
        /// <param name="panel">Panel to show.</param>
        void ShowPanel(UIPanel panel);

        /// <summary>Returns to the main menu (from pause or game over).</summary>
        void GoToMenu();

        /// <summary>Fills the game over screen with the final score.</summary>
        /// <param name="score">Score of the finished run.</param>
        /// <param name="bestScore">Best score after the run.</param>
        /// <param name="isNewRecord">Whether the run beat the record.</param>
        void ShowGameOver(int score, int bestScore, bool isNewRecord);

        /// <summary>Updates the HUD score text.</summary>
        /// <param name="score">Current score.</param>
        void SetHudScore(int score);

        /// <summary>Updates the HUD distance text.</summary>
        /// <param name="distance">Distance in meters.</param>
        void SetHudDistance(float distance);

        /// <summary>Fills the options panel from the persisted settings.</summary>
        /// <param name="settings">Settings to display.</param>
        void ApplySettings(SettingsData settings);

        /// <summary>Reads the options panel into settings.</summary>
        /// <returns>The settings currently displayed.</returns>
        SettingsData CollectSettings();
    }
}
