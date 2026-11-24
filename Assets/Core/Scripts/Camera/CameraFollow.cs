using UnityEngine;
using PowerPulse.Core;

namespace PowerPulse.CameraSystem
{
    /// <summary>
    /// Keeps the fixed gameplay framing: it follows the player on the X axis
    /// (so the sphere stays centered when it dashes) while preserving the
    /// manually configured Y, Z and rotation from the Transform.
    /// This allows positioning the camera in the Scene View and having it
    /// follow only horizontally.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("Target")]

        [SerializeField, Tooltip("Player the camera follows. Leave empty to use the MainCamera's first found player.")]
        private Transform m_Target;

        [Header("Follow Settings")]

        [SerializeField, Tooltip("Horizontal offset applied to the target X (usually 0).")]
        private float m_FollowX = 0f;

        [SerializeField, Tooltip("If true, only follows target X. Y, Z and rotation are kept from the Transform (set manually in Editor).")]
        private bool m_ManualYZRotation = true;

        [Header("Legacy Offset (used when Manual YZ Rotation is false)")]

        [SerializeField, Tooltip("Vertical offset above the target.")]
        private float m_Height = 2f;

        [SerializeField, Tooltip("Additional vertical offset to control player screen position.")]
        private float m_VerticalOffset = 0f;

        [SerializeField, Tooltip("Distance behind the target.")]
        private float m_Distance = 3.6f;

        [Header("Smoothing")]

        [SerializeField, Tooltip("How fast the camera follows X (higher = tighter). 0 = instant.")]
        private float m_Smoothing = 8f;

        #endregion


        #region FIELDS

        private float m_CurrentFollowX;
        private float m_BaseY;
        private float m_BaseZ;
        private IGameManager m_GameManager;

        #endregion


        #region PROPERTIES

        /// <summary>
        /// X position calculated by the follow this frame. Y/Z/rotation are from stored base values.
        /// The <c>CameraFX</c> applies its shake on top of this position.
        /// </summary>
        public Vector3 FollowPosition { get; private set; }

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            // Store base Y/Z from Transform (set in Editor/Scene View)
            // These are NEVER read from transform.position again (avoids CameraFX shake leak)
            m_BaseY = transform.position.y;
            m_BaseZ = transform.position.z;

            FollowPosition = new Vector3(transform.position.x, m_BaseY, m_BaseZ);
            m_CurrentFollowX = FollowPosition.x;

            m_GameManager = ServiceLocator.Current != null ? ServiceLocator.Current.Get<IGameManager>() : null;
        }

        private void LateUpdate()
        {
            // Stop following on GameOver (prevents camera falling with dead player)
            if (m_GameManager != null && m_GameManager.CurrentState == GameState.GameOver)
            {
                return;
            }

            if (m_Target == null)
            {
                return;
            }

            // Calculate desired X only
            float desiredX = m_Target.position.x + m_FollowX;

            float newFollowX;
            if (m_Smoothing <= 0f)
            {
                newFollowX = desiredX;
            }
            else
            {
                newFollowX = Mathf.Lerp(m_CurrentFollowX, desiredX, m_Smoothing * Time.deltaTime);
            }

            m_CurrentFollowX = newFollowX;

            if (m_ManualYZRotation)
            {
                // Manual mode: use STORED base Y/Z (never read from transform.position which has shake)
                FollowPosition = new Vector3(newFollowX, m_BaseY, m_BaseZ);
                transform.position = FollowPosition;
                // Rotation unchanged
            }
            else
            {
                // Legacy mode: compute full position from offsets
                Vector3 offset = new Vector3(m_FollowX, m_Height, -m_Distance);
                Vector3 desired = new Vector3(
                    m_Target.position.x + offset.x,
                    m_Target.position.y + offset.y + m_VerticalOffset,
                    m_Target.position.z + offset.z);

                if (m_Smoothing <= 0f)
                {
                    FollowPosition = desired;
                    transform.position = desired;
                    return;
                }

                FollowPosition = Vector3.Lerp(FollowPosition, desired, m_Smoothing * Time.deltaTime);
                transform.position = FollowPosition;
            }
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

        /// <summary>
        /// Syncs stored base Y/Z from current Transform position.
        /// Call this from Editor script or context menu if you manually moved the camera during Play mode
        /// and want to update the follow base position.
        /// </summary>
        [ContextMenu("Sync Base Y/Z From Transform")]
        public void SyncBaseFromTransform()
        {
            m_BaseY = transform.position.y;
            m_BaseZ = transform.position.z;
            FollowPosition = new Vector3(FollowPosition.x, m_BaseY, m_BaseZ);
        }

        #endregion
    }
}
