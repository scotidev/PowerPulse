using System;
using UnityEngine;

namespace PowerPulse.Player
{
    /// <summary>
    /// Detects trigger contacts with obstacles, power-ups and portals.
    /// The collection/death rules are implemented in the following phases.
    /// </summary>
    public class PlayerCollision : MonoBehaviour
    {
        #region EVENTS

        /// <summary>Raised when the player touches another trigger.</summary>
        public event Action<Collider> OnTriggerTouched;

        #endregion


        #region UNITY CALLBACKS

        private void OnTriggerEnter(Collider other)
        {
            OnTriggerTouched?.Invoke(other);
        }

        #endregion
    }
}
