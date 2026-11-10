namespace PowerPulse.Core
{
    /// <summary>
    /// The types of power-ups available in the game.
    /// </summary>
    public enum PowerUpType
    {
        /// <summary>Absorbs one incompatible collision.</summary>
        Shield,

        /// <summary>Allows collecting two colors for a limited time.</summary>
        BoostTwoColors,

        /// <summary>Allows collecting three colors for a limited time.</summary>
        BoostThreeColors,

        /// <summary>Allows collecting any color for a limited time.</summary>
        BoostAllColors
    }
}
