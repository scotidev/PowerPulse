namespace PowerPulse.Core
{
    /// <summary>
    /// Every top-level state the game can be in.
    /// This is only the list of values — the manager that uses it is <see cref="GameManager"/>.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Tutorial,
        Playing,
        Paused,
        GameOver
    }
}
