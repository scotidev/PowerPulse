using System.Collections.Generic;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Catalog of every obstacle shape available in the game. Each entry is a
    /// prefab that carries an Obstacle component defining its size and score.
    /// The color is assigned at spawn time by the SpawnManager.
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/Obstacle Catalog")]
    public class SO_ObstacleCatalog : ScriptableObject
    {
        #region SERIALIZED FIELDS

        [SerializeField, Tooltip("Obstacle shape prefabs available in the game.")]
        private List<GameObject> m_ObstaclePrefabs = new List<GameObject>();

        #endregion


        #region PROPERTIES

        /// <summary>Obstacle shape prefabs available in the game.</summary>
        public List<GameObject> ObstaclePrefabs => m_ObstaclePrefabs;

        #endregion
    }
}
