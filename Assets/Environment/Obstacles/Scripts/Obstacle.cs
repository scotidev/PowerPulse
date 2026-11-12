using UnityEngine;
using PowerPulse.Core;

namespace PowerPulse.Obstacles
{
    /// <summary>
    /// Visual and data representation of an obstacle. The size is read from the
    /// prefab and the score is calculated from it. The color is assigned at spawn
    /// time by the SpawnManager.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("Size")]
        [SerializeField, Tooltip("Size of the obstacle in units (width x height). Leave as (0, 0) to auto-detect from the mesh.")]
        private Vector2 m_Size = Vector2.zero;

        [Header("Visual")]
        [SerializeField, Tooltip("Color palette used to tint the obstacle.")]
        private SO_ColorPalette m_Palette;

        [SerializeField, Tooltip("Renderer that receives the obstacle color.")]
        private Renderer m_Renderer;

        #endregion


        #region FIELDS

        private MaterialPropertyBlock m_PropertyBlock;

        #endregion


        #region PROPERTIES

        /// <summary>Size of the obstacle in units (width x height).</summary>
        public Vector2 Size => m_Size;

        /// <summary>Score value awarded when the obstacle is collected (width x height).</summary>
        public int ScoreValue => Mathf.RoundToInt(m_Size.x * m_Size.y);

        /// <summary>Color currently applied to the obstacle.</summary>
        public ColorId ActiveColor { get; private set; }

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_PropertyBlock = new MaterialPropertyBlock();

            if (m_Renderer == null)
            {
                m_Renderer = GetComponentInChildren<Renderer>();
            }

            if (m_Size == Vector2.zero && m_Renderer != null)
            {
                m_Size = new Vector2(m_Renderer.bounds.size.x, m_Renderer.bounds.size.y);
            }
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Applies the given color to the obstacle.
        /// </summary>
        /// <param name="color">Color to apply.</param>
        public void SetColor(ColorId color)
        {
            ActiveColor = color;
            ApplyColor();
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Applies the active color to the renderer using a property block,
        /// avoiding per-instance material allocations.
        /// </summary>
        private void ApplyColor()
        {
            if (m_Renderer == null)
            {
                return;
            }

            Color color = m_Palette != null ? m_Palette.GetColor(ActiveColor) : Color.white;
            float emission = m_Palette != null ? m_Palette.GetEmission(ActiveColor) : 1f;

            m_PropertyBlock.SetColor("_BaseColor", color);
            m_PropertyBlock.SetColor("_EmissionColor", color * emission);
            m_Renderer.SetPropertyBlock(m_PropertyBlock);
        }

        #endregion
    }
}
