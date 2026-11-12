using UnityEngine;

namespace PowerPulse.Environment
{
    /// <summary>
    /// Marks a track tile prefab and exposes its length so the TrackManager can
    /// space and recycle tiles without hardcoding the value.
    /// </summary>
    public class TrackTile : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [SerializeField, Tooltip("Length of the tile in world units.")]
        private float m_Length = 10f;

        #endregion


        #region PROPERTIES

        /// <summary>Length of the tile in world units.</summary>
        public float Length => m_Length;

        #endregion
    }
}
