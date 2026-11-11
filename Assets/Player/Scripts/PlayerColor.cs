using System;
using UnityEngine;
using PowerPulse.Core;

namespace PowerPulse.Player
{
    /// <summary>
    /// Holds the active color of the player and applies it to the material.
    /// The unlock/boost/shield rules are implemented in the following phases.
    /// </summary>
    public class PlayerColor : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("References")]
        [SerializeField, Tooltip("Color palette asset.")]
        private SO_ColorPalette m_Palette;

        [SerializeField, Tooltip("Renderer that receives the active color.")]
        private Renderer m_Renderer;

        [Header("State")]
        [SerializeField, Tooltip("Color applied on Start.")]
        private ColorId m_InitialColor = ColorId.Red;

        #endregion


        #region FIELDS

        private Material m_MaterialInstance;

        #endregion


        #region PROPERTIES

        /// <summary>Currently active color.</summary>
        public ColorId ActiveColor { get; private set; }

        #endregion


        #region EVENTS

        /// <summary>Raised when the active color changes.</summary>
        public event Action<ColorId> OnColorChanged;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            if (m_Renderer != null)
            {
                m_MaterialInstance = m_Renderer.material;
            }
        }

        private void Start()
        {
            SetColor(m_InitialColor);
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>Changes the active color and applies it to the material.</summary>
        /// <param name="id">New color identifier.</param>
        public void SetColor(ColorId id)
        {
            ActiveColor = id;

            Color color = m_Palette.GetColor(id);
            float emission = m_Palette.GetEmission(id);

            m_MaterialInstance.color = color;
            m_MaterialInstance.SetColor("_BaseColor", color);
            m_MaterialInstance.SetColor("_EmissionColor", color * emission);

            OnColorChanged?.Invoke(id);
        }

        #endregion
    }
}
