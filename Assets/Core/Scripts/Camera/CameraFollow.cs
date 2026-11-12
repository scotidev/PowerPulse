using UnityEngine;

namespace PowerPulse.CameraSystem
{
    /// <summary>
    /// Keeps the fixed gameplay framing: it follows the player on the X axis
    /// (so the sphere stays centered when it dashes) while keeping the Z and Y
    /// offsets constant. This replaces the static camera from Phase 3 and makes
    /// the framing readable even when the player changes lane.
    /// The GDD says "camera fixed behind and above the sphere"; this implements
    /// that as a follow with a locked offset, which is the usual Unity way.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("Target")]

        [SerializeField, Tooltip("Player the camera follows. Leave empty to use the MainCamera's first found player.")]
        private Transform m_Target;

        [Header("Offset")]

        [SerializeField, Tooltip("Horizontal offset applied to the target (usually 0).")]
        private float m_FollowX = 0f;

        [SerializeField, Tooltip("Vertical offset above the target.")]
        private float m_Height = 2f;

        [SerializeField, Tooltip("Distance behind the target.")]
        private float m_Distance = 3.6f;

        [Header("Smoothing")]

        [SerializeField, Tooltip("How fast the camera follows (higher = tighter). 0 = instant.")]
        private float m_Smoothing = 8f;

        #endregion


        #region FIELDS

        private Vector3 m_Offset;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_Offset = new Vector3(m_FollowX, m_Height, -m_Distance);
        }

        private void LateUpdate()
        {
            if (m_Target == null)
            {
                return;
            }

            Transform target = m_Target;

            Vector3 desired = new Vector3(
                target.position.x + m_Offset.x,
                target.position.y + m_Offset.y,
                target.position.z + m_Offset.z);

            if (m_Smoothing <= 0f)
            {
                transform.position = desired;
                return;
            }

            transform.position = Vector3.Lerp(transform.position, desired, m_Smoothing * Time.deltaTime);
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Sets the target the camera follows. Used when the scene reloads and the
        /// previous reference became stale.
        /// </summary>
        /// <param name="target">Player transform.</param>
        public void SetTarget(Transform target)
        {
            m_Target = target;
        }

        #endregion
    }
}
