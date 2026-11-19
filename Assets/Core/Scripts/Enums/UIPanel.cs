namespace PowerPulse.Core
{
    /// <summary>
    /// Every panel the UIManager can show. Exactly one panel is visible per
    /// game state; Options, Controls and Credits are overlays on top of it.
    /// </summary>
    public enum UIPanel
    {
        /// <summary>In-run HUD: score, distance, active color, shield and boost.</summary>
        HUD,

        /// <summary>Main menu shown when the game is not running.</summary>
        MainMenu,

        /// <summary>Pause overlay with resume, options and quit actions.</summary>
        Pause,

        /// <summary>Game over screen with the final score and the restart action.</summary>
        GameOver,

        /// <summary>Options overlay with the persisted audio settings.</summary>
        Options,

        /// <summary>Controls overlay listing the input bindings.</summary>
        Controls,

        /// <summary>Credits overlay.</summary>
        Credits
    }
}
