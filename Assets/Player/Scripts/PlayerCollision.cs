using System;
using UnityEngine;
using PowerPulse.Core;
using PowerPulse.Obstacles;

namespace PowerPulse.Player
{
    /// <summary>
    /// Detects trigger contacts with obstacles and asks the player color state
    /// whether the touched color can be collected. It never decides rules or adds
    /// points itself — it only forwards the contact to the authority.
    /// Every obstacle collider is a trigger, so the contact arrives through
    /// <see cref="OnTriggerEnter"/> even though the player collider is not.
    /// </summary>
    public class PlayerCollision : MonoBehaviour
    {
        #region EVENTS

        /// <summary>Raised when the player touches another trigger.</summary>
        public event Action<Collider> OnTriggerTouched;

        #endregion


        #region FIELDS

        private PlayerColor m_PlayerColor;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            // The player state (color, shield, boost) lives on the same GameObject.
            // Asking it here keeps the ScoreManager out of this component.
            m_PlayerColor = GetComponent<PlayerColor>();

            if (m_PlayerColor == null)
            {
                Debug.LogError("[PlayerCollision] The player needs a PlayerColor component on the same GameObject.", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            OnTriggerTouched?.Invoke(other);

            Obstacle obstacle = other != null ? other.GetComponentInParent<Obstacle>() : null;

            if (obstacle == null)
            {
                return;
            }

            HandleObstacleTouch(obstacle);
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Forwards the contact to the player color state, which decides between
        /// collecting (compatible color) and dying (incompatible color).
        /// </summary>
        /// <param name="obstacle">Obstacle that touched the player.</param>
        private void HandleObstacleTouch(Obstacle obstacle)
        {
            if (m_PlayerColor == null)
            {
                return;
            }

            if (m_PlayerColor.CanCollect(obstacle.ActiveColor))
            {
                m_PlayerColor.Collect(obstacle);
                return;
            }

            m_PlayerColor.HandleIncompatibleHit(obstacle);
        }

        #endregion
    }
}
