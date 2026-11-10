using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Defines the five official colors of the game, their emission values and
    /// the order in which they are unlocked during a run.
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/Color Palette")]
    public class SO_ColorPalette : ScriptableObject
    {
        #region SERIALIZED FIELDS

        /// <summary>Serializable pair of color id, base color and emission.</summary>
        [Serializable]
        public class ColorEntry
        {
            [Tooltip("Identifier of the color.")]
            public ColorId Id;

            [Tooltip("Base color used by materials and VFX.")]
            public Color Color = Color.white;

            [Tooltip("Emission intensity applied to the material.")]
            public float EmissionIntensity = 1f;
        }

        [Header("Colors")]
        [SerializeField, Tooltip("The five official colors of the game.")]
        private List<ColorEntry> m_Colors = new List<ColorEntry>();

        [Header("Progression")]
        [SerializeField, Tooltip("Order in which colors are unlocked during a run.")]
        private List<ColorId> m_UnlockOrder = new List<ColorId>();

        #endregion


        #region PROPERTIES

        /// <summary>Number of colors defined in the palette.</summary>
        public int ColorCount => m_Colors.Count;

        #endregion


        #region PUBLIC METHODS

        /// <summary>Returns the palette entry for the given color.</summary>
        /// <param name="id">Color identifier.</param>
        /// <returns>The matching entry, or <c>null</c> when not found.</returns>
        public ColorEntry GetEntry(ColorId id)
        {
            return m_Colors.Find(entry => entry.Id == id);
        }

        /// <summary>Returns the base color for the given color id.</summary>
        /// <param name="id">Color identifier.</param>
        /// <returns>The base color, or white when not found.</returns>
        public Color GetColor(ColorId id)
        {
            ColorEntry entry = GetEntry(id);
            return entry != null ? entry.Color : Color.white;
        }

        /// <summary>Returns the emission intensity for the given color id.</summary>
        /// <param name="id">Color identifier.</param>
        /// <returns>The emission intensity, or 1 when not found.</returns>
        public float GetEmission(ColorId id)
        {
            ColorEntry entry = GetEntry(id);
            return entry != null ? entry.EmissionIntensity : 1f;
        }

        /// <summary>Returns the order in which colors are unlocked.</summary>
        /// <returns>The unlock order.</returns>
        public List<ColorId> GetUnlockOrder()
        {
            return m_UnlockOrder;
        }

        #endregion
    }
}
