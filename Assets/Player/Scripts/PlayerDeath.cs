using System;
using UnityEngine;
using PowerPulse.Core;

namespace PowerPulse.Player
{
    /// <summary>
    /// Owns the visible death of the player: it kills the rigidbody, stops the
    /// input reading and blinks the sphere for a short death animation before
    /// notifying the ScoreManager. It lives on the player, so the death is
    /// self-contained and does not depend on any screen existing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerDeath : MonoBehaviour
    {
        #region STATIC

        /// <summary>How long the death animation runs before the run is closed.</summary>
        private const float c_DeathAnimationDuration = 0.8f;

        /// <summary>Seconds between each blink of the sphere.</summary>
        private const float c_BlinkInterval = 0.1f;

        /// <summary>Scale of the sphere at rest.</summary>
        private const float c_RestScale = 0.8f;

        #endregion


        #region SERIALIZED FIELDS

        [Header("References")]

        [SerializeField, Tooltip("Score manager that closes the run and records the score.")]
        private ScoreManager m_ScoreManager;

        [SerializeField, Tooltip("Trail renderer disabled during the death animation.")]
        private TrailRenderer m_Trail;

        [Header("Death Animation")]

        [SerializeField, Tooltip("Total duration of the death animation in seconds.")]
        private float m_DeathDuration = c_DeathAnimationDuration;

        [SerializeField, Tooltip("Seconds between each blink of the sphere.")]
        private float m_BlinkInterval = c_BlinkInterval;

        #endregion


        #region FIELDS

        private Rigidbody m_Rb;
        private PlayerController m_Controller;
        private PlayerCollision m_Collision;
        private float m_Elapsed;
        private bool m_IsDying;
        private bool m_HasFinished;
        private int m_BlinkCount;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_Rb = GetComponent<Rigidbody>();
            m_Controller = GetComponent<PlayerController>();
            m_Collision = GetComponent<PlayerCollision>();
        }

        private void Update()
        {
            TickDeathAnimation();
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Starts the death animation. Called by the PlayerColor when the
        /// ScoreManager decides the player died.
        /// </summary>
        public void Die()
        {
            if (m_IsDying)
            {
                return;
            }

            m_IsDying = true;
            m_Elapsed = 0f;
            m_BlinkCount = 0;

            // The player stops reacting to the world: no input, no collision,
            // no gravity and no movement.
            if (m_Controller != null)
            {
                m_Controller.enabled = false;
            }

            if (m_Collision != null)
            {
                m_Collision.enabled = false;
            }

            if (m_Rb != null)
            {
                m_Rb.isKinematic = true;
                m_Rb.linearVelocity = Vector3.zero;
                m_Rb.angularVelocity = Vector3.zero;
            }

            if (m_Trail != null)
            {
                m_Trail.enabled = false;
                m_Trail.Clear();
            }

            Debug.Log("[PlayerDeath] Death animation started.");
        }

        /// <summary>
        /// Restores the player for a new run: re-enables the components, the
        /// rigidbody, the renderers and resets the scale.
        /// </summary>
        public void ResetPlayer()
        {
            m_IsDying = false;
            m_HasFinished = false;
            m_Elapsed = 0f;
            m_BlinkCount = 0;

            if (m_Controller != null)
            {
                m_Controller.enabled = true;
            }

            if (m_Collision != null)
            {
                m_Collision.enabled = true;
            }

            if (m_Rb != null)
            {
                m_Rb.isKinematic = false;
                m_Rb.linearVelocity = Vector3.zero;
                m_Rb.angularVelocity = Vector3.zero;
            }

            if (m_Trail != null)
            {
                m_Trail.enabled = true;
                m_Trail.Clear();
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = true;
            }

            if (m_Controller != null)
            {
                m_Controller.ResetMovement();
            }

            transform.localScale = Vector3.one * c_RestScale;
            gameObject.SetActive(true);

            Debug.Log("[PlayerDeath] Player restored for a new run.");
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Runs the blink animation and closes the run when it finishes.
        /// </summary>
        private void TickDeathAnimation()
        {
            if (!m_IsDying || m_HasFinished)
            {
                return;
            }

            m_Elapsed += Time.deltaTime;

            float blinkTimer = m_Elapsed - (m_BlinkCount * m_BlinkInterval);

            if (blinkTimer >= m_BlinkInterval)
            {
                m_BlinkCount++;
                ToggleSphereVisibility();
            }

            if (m_Elapsed < m_DeathDuration)
            {
                return;
            }

            m_HasFinished = true;
            gameObject.SetActive(false);

            if (m_ScoreManager != null)
            {
                m_ScoreManager.CloseRun();
            }

            Debug.Log("[PlayerDeath] Death animation finished. Run closed.");
        }

        /// <summary>
        /// Toggles the sphere visibility to create the blink effect.
        /// </summary>
        private void ToggleSphereVisibility()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = !renderers[i].enabled;
            }
        }

        #endregion
    }
}
