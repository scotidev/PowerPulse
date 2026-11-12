using System;
using System.Collections.Generic;
using UnityEngine;
using PowerPulse.Core;

namespace PowerPulse.Obstacles
{
    /// <summary>
    /// Visual and data representation of an obstacle. The size is read from the
    /// prefab and the score is calculated from it. The color and the behaviour
    /// are assigned at spawn time by the SpawnManager.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        #region STATIC

        /// <summary>Half-width of the track in world units, used to clamp the lateral movement.</summary>
        private const float c_TrackHalfWidth = 2f;

        #endregion


        #region SERIALIZED FIELDS

        [Header("Size")]

        [SerializeField, Tooltip("Size of the obstacle in units (width x height). Leave as (0, 0) to auto-detect from the mesh.")]
        private Vector2 m_Size = Vector2.zero;

        [Header("Visual")]

        [SerializeField, Tooltip("Color palette used to tint the obstacle.")]
        private SO_ColorPalette m_Palette;

        [SerializeField, Tooltip("Renderer that receives the obstacle color.")]
        private Renderer m_Renderer;

        [Header("Behaviour")]

        [SerializeField, Tooltip("How the obstacle behaves during the run.")]
        private ObstacleBehaviour m_Behaviour = ObstacleBehaviour.Static;

        [SerializeField, Tooltip("Lateral speed in units per second for Moving and Faster behaviours.")]
        private float m_MoveSpeed = 1.5f;

        #endregion


        #region FIELDS

        private MaterialPropertyBlock m_PropertyBlock;
        private float m_Direction = -1f;
        private bool m_IsMovable;

        #endregion


        #region PROPERTIES

        /// <summary>Size of the obstacle in units (width x height).</summary>
        public Vector2 Size => m_Size;

        /// <summary>Score value awarded when the obstacle is collected (width x height).</summary>
        public int ScoreValue => Mathf.RoundToInt(m_Size.x * m_Size.y);

        /// <summary>Color currently applied to the obstacle.</summary>
        public ColorId ActiveColor { get; private set; }

        /// <summary>Current behaviour of the obstacle.</summary>
        public ObstacleBehaviour Behaviour => m_Behaviour;

        /// <summary>Lateral speed in units per second.</summary>
        public float MoveSpeed => m_MoveSpeed;

        /// <summary>Whether the obstacle moves laterally during the run.</summary>
        public bool IsMovable => m_IsMovable;

        /// <summary>Prefab the instance was pooled from. Stored at spawn so the instance can be recycled.</summary>
        public GameObject SourcePrefab { get; set; }

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

        /// <summary>
        /// Assigns the behaviour of the obstacle at spawn time. Tall, Wide and
        /// Static stay in place; Moving and Faster travel from right to left,
        /// bouncing between the edges of the track.
        /// </summary>
        /// <param name="behaviour">Behaviour to apply.</param>
        /// <param name="moveSpeed">Lateral speed in units per second.</param>
        public void SetBehaviour(ObstacleBehaviour behaviour, float moveSpeed)
        {
            m_Behaviour = behaviour;
            m_MoveSpeed = Mathf.Max(0f, moveSpeed);
            m_IsMovable = behaviour == ObstacleBehaviour.Moving || behaviour == ObstacleBehaviour.Faster;
        }

        /// <summary>
        /// Moves the obstacle laterally by the given distance. Called by the
        /// SpawnManager every frame, right after the track movement.
        /// </summary>
        /// <param name="distance">Lateral distance in world units.</param>
        public void MoveLateral(float distance)
        {
            if (!m_IsMovable)
            {
                return;
            }

            float x = transform.position.x + distance * m_Direction;

            float halfWidth = m_Size.x * 0.5f;
            float limit = c_TrackHalfWidth - halfWidth;

            if (x <= -limit)
            {
                x = -limit;
                m_Direction = 1f;
            }
            else if (x >= limit)
            {
                x = limit;
                m_Direction = -1f;
            }

            transform.position = new Vector3(x, transform.position.y, transform.position.z);
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
