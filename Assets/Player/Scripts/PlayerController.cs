using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using PowerPulse.Core;

namespace PowerPulse.Player
{
    /// <summary>
    /// Single owner of the player position. Reads the input (keyboard and touch)
    /// and combines the lane dash with the jump in one calculation per frame.
    /// Holds the Rigidbody and the TrailRenderer
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        #region STATIC

        private const float c_LaneWidth = 1f;
        private const int c_MinLane = -2;
        private const int c_MaxLane = 2;

        #endregion


        #region SERIALIZED FIELDS

        [Header("Config")]
        [SerializeField, Tooltip("Input settings asset.")]
        private Config_InputSettings m_InputSettings;

        [SerializeField, Tooltip("Input actions asset (keyboard).")]
        private InputActionAsset m_InputActions;

        [Header("Ground Check")]
        [SerializeField, Tooltip("Layer mask used to detect the ground.")]
        private LayerMask m_GroundMask;

        [SerializeField, Tooltip("Distance from the player origin to the ground.")]
        private float m_GroundCheckDistance = 0.5f;

        [Header("Touch")]
        [SerializeField, Tooltip("Minimum vertical swipe distance (in pixels) to jump.")]
        private float m_SwipeThreshold = 50f;

        #endregion


        #region FIELDS

        private Rigidbody m_Rb;
        private TrailRenderer m_Trail;
        private InputAction m_DashLeftAction;
        private InputAction m_DashRightAction;
        private InputAction m_JumpAction;
        private int m_TargetLane;
        private bool m_IsGrounded;

        #endregion


        #region PROPERTIES

        /// <summary>Whether the player is touching the ground.</summary>
        public bool IsGrounded => m_IsGrounded;

        /// <summary>Current target lane (-2 to +2).</summary>
        public int CurrentLane => m_TargetLane;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_Rb = GetComponent<Rigidbody>();
            m_Trail = GetComponent<TrailRenderer>();

            m_DashLeftAction = m_InputActions.FindAction("DashLeft");
            m_DashRightAction = m_InputActions.FindAction("DashRight");
            m_JumpAction = m_InputActions.FindAction("Jump");

            m_DashLeftAction.Enable();
            m_DashRightAction.Enable();
            m_JumpAction.Enable();

            m_TargetLane = 0;
        }

        private void OnEnable()
        {
            ResetMovement();
        }

        private void Update()
        {
            ReadInput();
            CheckGround();
        }

        private void FixedUpdate()
        {
            ApplyLaneMovement();
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Resets the player position and lane for a new run.
        /// </summary>
        public void ResetMovement()
        {
            m_TargetLane = 0;

            if (m_Rb != null)
            {
                m_Rb.linearVelocity = Vector3.zero;
                m_Rb.angularVelocity = Vector3.zero;
            }

            transform.position = new Vector3(0f, transform.position.y, transform.position.z);

            if (m_Trail != null)
            {
                m_Trail.Clear();
            }
        }

        /// <summary>
        /// Moves the player exactly one lane in the given direction.
        /// </summary>
        /// <param name="direction">-1 for left, +1 for right.</param>
        public void Dash(int direction)
        {
            int newLane = Mathf.Clamp(m_TargetLane + direction, c_MinLane, c_MaxLane);
            if (newLane == m_TargetLane)
            {
                return;
            }

            m_TargetLane = newLane;
            StartCoroutine(DashTrailRoutine());
        }

        /// <summary>
        /// Applies the jump if the player is grounded.
        /// </summary>
        public void Jump()
        {
            if (!m_IsGrounded)
            {
                return;
            }

            m_Rb.linearVelocity = new Vector3(m_Rb.linearVelocity.x, m_InputSettings.JumpForce, m_Rb.linearVelocity.z);
            m_IsGrounded = false;
        }

        #endregion


        #region PRIVATE METHODS

        private void ReadInput()
        {
            if (m_DashLeftAction != null && m_DashLeftAction.WasPressedThisFrame())
            {
                Dash(-1);
            }

            if (m_DashRightAction != null && m_DashRightAction.WasPressedThisFrame())
            {
                Dash(1);
            }

            if (m_JumpAction != null && m_JumpAction.WasPressedThisFrame())
            {
                Jump();
            }

            ReadTouchInput();
        }

        private void ReadTouchInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            TouchControl touch = touchscreen.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                Vector2 position = touch.position.ReadValue();
                Dash(position.x < Screen.width * 0.5f ? -1 : 1);
            }

            if (touch.delta.ReadValue().y > m_SwipeThreshold)
            {
                Jump();
            }
        }

        private void CheckGround()
        {
            Vector3 origin = transform.position;
            m_IsGrounded = Physics.Raycast(origin, Vector3.down, m_GroundCheckDistance, m_GroundMask);
        }

        private void ApplyLaneMovement()
        {
            float targetX = m_TargetLane * c_LaneWidth;
            float currentX = transform.position.x;

            if (Mathf.Approximately(currentX, targetX))
            {
                return;
            }

            float speed = c_LaneWidth / Mathf.Max(m_InputSettings.DashDuration, 0.01f);
            float maxStep = speed * Time.fixedDeltaTime;
            float newX = Mathf.MoveTowards(currentX, targetX, maxStep);

            m_Rb.MovePosition(new Vector3(newX, m_Rb.position.y, m_Rb.position.z));
        }

        /// <summary>Enables the trail during the dash and disables it afterwards.</summary>
        private IEnumerator DashTrailRoutine()
        {
            if (m_Trail == null)
            {
                yield break;
            }

            m_Trail.enabled = true;
            yield return new WaitForSeconds(m_InputSettings.DashDuration);
            m_Trail.Clear();
            m_Trail.enabled = false;
        }

        #endregion
    }
}
