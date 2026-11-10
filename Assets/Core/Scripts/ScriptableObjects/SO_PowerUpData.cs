using System.Collections.Generic;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Data-driven definition of a power-up: type, duration, covered colors and
    /// feedback assets. One asset is created for each power-up type.
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/Power-Up Data")]
    public class SO_PowerUpData : ScriptableObject
    {
        #region SERIALIZED FIELDS

        [Header("Identity")]
        [SerializeField, Tooltip("Type of the power-up.")]
        private PowerUpType m_Type = PowerUpType.Shield;

        [SerializeField, Tooltip("Duration of the effect in seconds (0 = instant).")]
        private float m_Duration = 0f;

        [Header("Boost")]
        [SerializeField, Tooltip("Colors covered by the boost.")]
        private List<ColorId> m_ColorsCovered = new List<ColorId>();

        [Header("Feedback")]
        [SerializeField, Tooltip("Sound played when the power-up is collected.")]
        private AudioClip m_Sfx;

        [SerializeField, Tooltip("Visual effect spawned when the power-up is collected.")]
        private GameObject m_VfxPrefab;

        #endregion


        #region PROPERTIES

        /// <summary>Type of the power-up.</summary>
        public PowerUpType Type => m_Type;

        /// <summary>Duration of the effect in seconds.</summary>
        public float Duration => m_Duration;

        /// <summary>Colors covered by the boost.</summary>
        public List<ColorId> ColorsCovered => m_ColorsCovered;

        /// <summary>Sound played when the power-up is collected.</summary>
        public AudioClip Sfx => m_Sfx;

        /// <summary>Visual effect spawned when the power-up is collected.</summary>
        public GameObject VfxPrefab => m_VfxPrefab;

        #endregion
    }
}
