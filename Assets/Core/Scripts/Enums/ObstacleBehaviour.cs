namespace PowerPulse.Core
{
    /// <summary>
    /// How an obstacle behaves during the run.
    /// </summary>
    public enum ObstacleBehaviour
    {
        /// <summary>Stays in place for the whole run.</summary>
        Static,

        /// <summary>Moves laterally from right to left.</summary>
        Moving,

        /// <summary>Moves laterally at a higher speed.</summary>
        Faster,

        /// <summary>Tall obstacle, harder to jump over.</summary>
        Tall,

        /// <summary>Wide obstacle, occupies more than one lane.</summary>
        Wide
    }
}
