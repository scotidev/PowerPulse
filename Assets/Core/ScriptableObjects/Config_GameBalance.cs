using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Global balance configuration: speed progression, spawn rules and
    /// difficulty curves. Adjusted by the Inspector, never hardcoded.
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/Game Balance")]
    public class Config_GameBalance : ScriptableObject
    {
        #region SERIALIZED FIELDS

        [Header("Speed")]
        [SerializeField, Tooltip("Track speed at the start of a run (units per second).")]
        private float m_InitialSpeed = 5f;

        [SerializeField, Tooltip("Maximum track speed (units per second).")]
        private float m_MaxSpeed = 20f;

        [SerializeField, Tooltip("Speed curve over distance (x = distance, y = speed multiplier).")]
        private AnimationCurve m_SpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Spawning")]
        [SerializeField, Tooltip("Distance between color portals (meters).")]
        private float m_PortalInterval = 500f;

        [SerializeField, Tooltip("Average distance between power-up spawns (meters).")]
        private float m_PowerUpInterval = 250f;

        [SerializeField, Tooltip("Minimum distance between obstacle spawns (meters).")]
        private float m_MinSpawnDistance = 3f;

        [SerializeField, Tooltip("Density curve over distance (x = distance, y = density multiplier).")]
        private AnimationCurve m_DensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField, Tooltip("Maximum number of colors active at once.")]
        private int m_MaxColors = 5;

        #endregion


        #region PROPERTIES

        /// <summary>Track speed at the start of a run (units per second).</summary>
        public float InitialSpeed => m_InitialSpeed;

        /// <summary>Maximum track speed (units per second).</summary>
        public float MaxSpeed => m_MaxSpeed;

        /// <summary>Speed curve over distance.</summary>
        public AnimationCurve SpeedCurve => m_SpeedCurve;

        /// <summary>Distance between color portals (meters).</summary>
        public float PortalInterval => m_PortalInterval;

        /// <summary>Average distance between power-up spawns (meters).</summary>
        public float PowerUpInterval => m_PowerUpInterval;

        /// <summary>Minimum distance between obstacle spawns (meters).</summary>
        public float MinSpawnDistance => m_MinSpawnDistance;

        /// <summary>Density curve over distance.</summary>
        public AnimationCurve DensityCurve => m_DensityCurve;

        /// <summary>Maximum number of colors active at once.</summary>
        public int MaxColors => m_MaxColors;

        #endregion
    }
}
