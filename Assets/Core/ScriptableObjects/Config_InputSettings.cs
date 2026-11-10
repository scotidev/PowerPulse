using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Input configuration: swipe sensitivity, touch thresholds and movement
    /// parameters for dash and jump.
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/Input Settings")]
    public class Config_InputSettings : ScriptableObject
    {
        #region SERIALIZED FIELDS

        [Header("Touch")]
        [SerializeField, Tooltip("Minimum swipe distance (in pixels) to register a swipe.")]
        private float m_SwipeSensitivity = 50f;

        [SerializeField, Tooltip("Maximum touch movement (in pixels) to be considered a tap.")]
        private float m_TouchThreshold = 20f;

        [Header("Dash")]
        [SerializeField, Tooltip("Duration of a lane dash (seconds).")]
        private float m_DashDuration = 0.2f;

        [SerializeField, Tooltip("Speed multiplier applied during a dash.")]
        private float m_DashSpeed = 3f;

        [Header("Jump")]
        [SerializeField, Tooltip("Vertical force applied when jumping.")]
        private float m_JumpForce = 10f;

        #endregion


        #region PROPERTIES

        /// <summary>Minimum swipe distance (in pixels) to register a swipe.</summary>
        public float SwipeSensitivity => m_SwipeSensitivity;

        /// <summary>Maximum touch movement (in pixels) to be considered a tap.</summary>
        public float TouchThreshold => m_TouchThreshold;

        /// <summary>Duration of a lane dash (seconds).</summary>
        public float DashDuration => m_DashDuration;

        /// <summary>Speed multiplier applied during a dash.</summary>
        public float DashSpeed => m_DashSpeed;

        /// <summary>Vertical force applied when jumping.</summary>
        public float JumpForce => m_JumpForce;

        #endregion
    }
}
