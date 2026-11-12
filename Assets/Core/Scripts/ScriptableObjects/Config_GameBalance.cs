using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Global balance configuration: speed progression, spawn rules, difficulty
    /// milestones and portal rules. Adjusted by the Inspector, never hardcoded.
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/Game Balance")]
    public class Config_GameBalance : ScriptableObject
    {
        #region NESTED TYPES

        /// <summary>
        /// A single difficulty milestone: when it triggers and what it unlocks.
        /// </summary>
        [Serializable]
        public class DifficultyMilestone
        {
            [Tooltip("Distance in meters where this milestone triggers.")]
            public float Distance = 500f;

            [Tooltip("Track speed applied from this milestone on (units per second).")]
            public float Speed = 8f;

            [Tooltip("Whether lateral moving obstacles are enabled from here on.")]
            public bool EnableMoving;

            [Tooltip("Whether faster lateral obstacles are enabled from here on.")]
            public bool EnableFaster;

            [Tooltip("Maximum number of obstacles per row from here on.")]
            public int MaxObstaclesPerRow = 1;
        }

        #endregion


        #region SERIALIZED FIELDS

        [Header("Speed")]
        [SerializeField, Tooltip("Track speed at the start of a run (units per second).")]
        private float m_InitialSpeed = 5f;

        [SerializeField, Tooltip("Maximum track speed (units per second).")]
        private float m_MaxSpeed = 20f;

        [SerializeField, Tooltip("Speed curve over distance (x = normalized distance, y = speed multiplier).")]
        private AnimationCurve m_SpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField, Tooltip("Distance (meters) over which the speed curve goes from 0 to 1.")]
        private float m_SpeedDistanceReference = 5000f;

        [Header("Difficulty Milestones")]

        [SerializeField, Tooltip("Ordered list of difficulty milestones, by distance. The RunManager reacts to them.")]
        private List<DifficultyMilestone> m_Milestones = new List<DifficultyMilestone>();

        [Header("Spawning")]

        [SerializeField, Tooltip("Distance between color portals (meters).")]
        private float m_PortalInterval = 500f;

        [SerializeField, Tooltip("Minimum distance between an obstacle row and an active portal (meters).")]
        private float m_PortalClearance = 40f;

        [SerializeField, Tooltip("Extra clearance applied ahead of the portal (meters), because the obstacles that already spawned are still traveling toward it.")]
        private float m_PortalClearanceAhead = 20f;

        [SerializeField, Tooltip("Distance traveled after a portal before obstacles spawn again (adaptation window, meters).")]
        private float m_PortalCooldown = 25f;

        [SerializeField, Tooltip("Minimum distance between an obstacle row and a portal (meters).")]
        private float m_PortalObstacleGap = 10f;

        [SerializeField, Tooltip("Average distance between power-up spawns (meters).")]
        private float m_PowerUpInterval = 250f;

        [SerializeField, Tooltip("Minimum distance between obstacle spawns (meters).")]
        private float m_MinSpawnDistance = 3f;

        [SerializeField, Tooltip("Density curve over distance (x = distance, y = density multiplier).")]
        private AnimationCurve m_DensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField, Tooltip("Maximum number of colors active at once.")]
        private int m_MaxColors = 5;

        [Header("Obstacle Movement")]

        [SerializeField, Tooltip("Lateral speed of Moving obstacles in units per second.")]
        private float m_MovingSpeed = 1.5f;

        [SerializeField, Tooltip("Lateral speed of Faster obstacles in units per second.")]
        private float m_FasterSpeed = 3f;

        #endregion


        #region PROPERTIES

        /// <summary>Track speed at the start of a run (units per second).</summary>
        public float InitialSpeed => m_InitialSpeed;

        /// <summary>Maximum track speed (units per second).</summary>
        public float MaxSpeed => m_MaxSpeed;

        /// <summary>Speed curve over distance.</summary>
        public AnimationCurve SpeedCurve => m_SpeedCurve;

        /// <summary>Distance (meters) over which the speed curve goes from 0 to 1.</summary>
        public float SpeedDistanceReference => m_SpeedDistanceReference;

        /// <summary>Ordered list of difficulty milestones.</summary>
        public List<DifficultyMilestone> Milestones => m_Milestones;

        /// <summary>Distance between color portals (meters).</summary>
        public float PortalInterval => m_PortalInterval;

        /// <summary>Minimum distance between an obstacle row and an active portal (meters).</summary>
        public float PortalClearance => m_PortalClearance;

        /// <summary>Extra clearance applied ahead of the portal (meters).</summary>
        public float PortalClearanceAhead => m_PortalClearanceAhead;

        /// <summary>Distance traveled after a portal before obstacles spawn again (meters).</summary>
        public float PortalCooldown => m_PortalCooldown;

        /// <summary>Minimum distance between an obstacle row and a portal (meters).</summary>
        public float PortalObstacleGap => m_PortalObstacleGap;

        /// <summary>Average distance between power-up spawns (meters).</summary>
        public float PowerUpInterval => m_PowerUpInterval;

        /// <summary>Minimum distance between obstacle spawns (meters).</summary>
        public float MinSpawnDistance => m_MinSpawnDistance;

        /// <summary>Density curve over distance.</summary>
        public AnimationCurve DensityCurve => m_DensityCurve;

        /// <summary>Maximum number of colors active at once.</summary>
        public int MaxColors => m_MaxColors;

        /// <summary>Lateral speed of Moving obstacles in units per second.</summary>
        public float MovingSpeed => m_MovingSpeed;

        /// <summary>Lateral speed of Faster obstacles in units per second.</summary>
        public float FasterSpeed => m_FasterSpeed;

        #endregion
    }
}
