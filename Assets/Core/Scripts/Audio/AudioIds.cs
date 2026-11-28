namespace PowerPulse.Audio
{
    /// <summary>
    /// Music track identifiers. Used by IAudioService.PlayMusic and CrossfadeTo.
    /// Order matches the expected array in SO_AudioCatalog.
    /// </summary>
    public enum MusicId
    {
        /// <summary>Main menu music (loops).</summary>
        Menu = 0,

        /// <summary>Gameplay music track 1 (shuffled with 2 and 3).</summary>
        Gameplay_1 = 1,

        /// <summary>Gameplay music track 2 (shuffled with 1 and 3).</summary>
        Gameplay_2 = 2,

        /// <summary>Gameplay music track 3 (shuffled with 1 and 2).</summary>
        Gameplay_3 = 3,

        /// <summary>Death screen music (loops until retry or quit).</summary>
        Death = 4
    }

    /// <summary>
    /// Sound effect identifiers. Used by IAudioService.PlaySfx.
    /// </summary>
    public enum SfxId
    {
        // === UI ===
        /// <summary>Click on any UI button.</summary>
        UI_Click = 0,

        /// <summary>Game paused (pause menu opened).</summary>
        UI_Pause = 1,

        /// <summary>Game unpaused (resume from pause).</summary>
        UI_Unpause = 2,

        // === GAMEPLAY CORE ===
        /// <summary>Collecting an obstacle of the correct color.</summary>
        Collect_Correct = 3,

        /// <summary>Player dash (lane change).</summary>
        Dash = 4,

        /// <summary>Player jump.</summary>
        Jump = 5,

        /// <summary>Passing through a color portal.</summary>
        Portal = 6,

        // === POWER-UPS ===
        /// <summary>Collecting the Shield power-up.</summary>
        PowerUp_Shield_Collect = 7,

        /// <summary>Shield breaking (absorbing one incompatible hit).</summary>
        PowerUp_Shield_Break = 8,

        /// <summary>Collecting the CollectAllColors power-up.</summary>
        PowerUp_CollectAllColors_Collect = 9,

        // === COLLECT ALL COLORS BOOST ===
        /// <summary>
        /// Looping tick sound during CollectAllColors boost.
        /// Pitch is modulated at runtime (1.0 -> 2.5) to indicate time remaining.
        /// </summary>
        CollectAllColors_Tick = 10,

        /// <summary>One-shot when CollectAllColors boost ends.</summary>
        CollectAllColors_End = 11,

        // === DEATH SEQUENCE ===
        /// <summary>First impact with wrong-color obstacle (hit sound).</summary>
        Death_Hit = 12,

        /// <summary>Sphere inflating before explosion.</summary>
        Death_Inflate = 13,

        /// <summary>Sphere exploding (final death).</summary>
        Death_Explode = 14,

        // === UI ===
        /// <summary>Mouse hover over a UI button.</summary>
        UI_Hover = 15
    }
}