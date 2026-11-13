using UnityEngine;
using PowerPulse.Core;

namespace PowerPulse.Environment
{
    /// <summary>
    /// A translucent arc that crosses the whole track. The player cannot dodge
    /// it: crossing it changes the active color and unlocks the next color in
    /// the palette order. Recycled through the shared ObjectPool.
    /// This component only detects the crossing; the SpawnManager (which lives
    /// in the scene) applies the color change, because a prefab cannot reference
    /// scene objects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Portal : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("Visual")]

        [SerializeField, Tooltip("Color palette used to tint the portal.")]
        private SO_ColorPalette m_Palette;

        [SerializeField, Tooltip("Renderer that receives the portal color.")]
        private Renderer m_Renderer;

        [Header("Rules")]

        [SerializeField, Tooltip("Color the player will assume when crossing this portal.")]
        private ColorId m_TargetColor = ColorId.Blue;

        #endregion


        #region FIELDS

        private MaterialPropertyBlock m_PropertyBlock;
        private SpawnManager m_Manager;
        private bool m_HasBeenCrossed;

        #endregion


        #region PROPERTIES

        /// <summary>Color the player will assume when crossing this portal.</summary>
        public ColorId TargetColor => m_TargetColor;

        /// <summary>Whether the player already crossed this portal.</summary>
        public bool HasBeenCrossed => m_HasBeenCrossed;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_PropertyBlock = new MaterialPropertyBlock();

            if (m_Renderer == null)
            {
                m_Renderer = GetComponentInChildren<Renderer>();
            }

            ApplyColor();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_HasBeenCrossed)
            {
                return;
            }

            if (!other.CompareTag("Player"))
            {
                return;
            }

            Cross();
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Configures the portal for a new spawn: the color it will grant, the
        /// state that resets the crossing flag and the manager that owns the rule.
        /// </summary>
        /// <param name="color">Color the player will assume.</param>
        /// <param name="manager">Manager that applies the color change.</param>
        public void SetPortal(ColorId color, SpawnManager manager)
        {
            m_TargetColor = color;
            m_Manager = manager;
            m_HasBeenCrossed = false;
            ApplyColor();
        }

        /// <summary>
        /// Resets the crossing flag so the portal can be reused from the pool.
        /// </summary>
        public void ResetPortal()
        {
            m_HasBeenCrossed = false;
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Notifies the manager that the player crossed this portal. The manager
        /// owns the color change and the unlock, because it is the scene object
        /// that knows the ScoreManager.
        /// </summary>
        private void Cross()
        {
            m_HasBeenCrossed = true;

            if (m_Manager == null)
            {
                Debug.LogError("[Portal] The SpawnManager is not assigned. The color cannot change.", this);
                return;
            }

            m_Manager.HandlePortalCrossed(this);
        }

        /// <summary>
        /// Applies the target color to the renderer using a property block,
        /// avoiding per-instance material allocations. The transparency itself is
        /// authored on the portal material in the Editor.
        /// </summary>
        private void ApplyColor()
        {
            if (m_Renderer == null)
            {
                return;
            }

            Color color = m_Palette != null ? m_Palette.GetColor(m_TargetColor) : Color.white;
            float emission = m_Palette != null ? m_Palette.GetEmission(m_TargetColor) : 1f;

            m_PropertyBlock.SetColor("_BaseColor", color);
            m_PropertyBlock.SetColor("_EmissionColor", color * emission);
            m_Renderer.SetPropertyBlock(m_PropertyBlock);
        }

        #endregion
    }
}
