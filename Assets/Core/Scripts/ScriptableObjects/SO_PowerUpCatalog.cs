using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Catalog of every power-up available in the game. Each entry pairs a prefab
    /// with its data (type, duration, colors, sfx and vfx), so a power-up is
    /// configured entirely in this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/Power-Up Catalog")]
    public class SO_PowerUpCatalog : ScriptableObject
    {
        #region NESTED TYPES

        /// <summary>A single power-up definition: the prefab and its data.</summary>
        [Serializable]
        public class PowerUpEntry
        {
            [Tooltip("Power-up prefab.")]
            public GameObject Prefab;

            [Tooltip("Type of the power-up.")]
            public PowerUpType Type = PowerUpType.Shield;

            [Tooltip("Duration of the effect in seconds (0 = instant).")]
            public float Duration = 0f;

            [Tooltip("Colors covered by the boost. Fallback when there are not enough unlocked colors.")]
            public List<ColorId> ColorsCovered = new List<ColorId>();

            [Tooltip("Sound played when the power-up is collected.")]
            public AudioClip Sfx;

            [Tooltip("Visual effect spawned when the power-up is collected.")]
            public GameObject VfxPrefab;
        }

        #endregion


        #region SERIALIZED FIELDS

        [SerializeField, Tooltip("Power-up definitions available in the game.")]
        private List<PowerUpEntry> m_PowerUps = new List<PowerUpEntry>();

        #endregion


        #region PROPERTIES

        /// <summary>Power-up definitions available in the game.</summary>
        public List<PowerUpEntry> PowerUps => m_PowerUps;

        #endregion
    }
}
