using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Data-driven definition of an obstacle type: color, size, score value and
    /// behavior. One asset is created for each obstacle variant.
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/Obstacle Data")]
    public class SO_ObstacleData : ScriptableObject
    {
        #region SERIALIZED FIELDS

        [Header("Identity")]
        [SerializeField, Tooltip("Color of the obstacle.")]
        private ColorId m_Color = ColorId.Red;

        [Header("Size")]
        [SerializeField, Tooltip("Size of the obstacle in units (width x height).")]
        private Vector2 m_Size = Vector2.one;

        [SerializeField, Tooltip("Score value awarded when collected (equals the area).")]
        private int m_ScoreValue = 1;

        [Header("Behavior")]
        [SerializeField, Tooltip("How the obstacle behaves during the run.")]
        private ObstacleBehaviour m_Behaviour = ObstacleBehaviour.Static;

        [SerializeField, Tooltip("Whether the obstacle can be avoided by jumping.")]
        private bool m_CanBeJumped = true;

        [SerializeField, Tooltip("Lateral movement speed for moving obstacles.")]
        private float m_MoveSpeed = 1f;

        #endregion


        #region PROPERTIES

        /// <summary>Color of the obstacle.</summary>
        public ColorId Color => m_Color;

        /// <summary>Size of the obstacle in units (width x height).</summary>
        public Vector2 Size => m_Size;

        /// <summary>Score value awarded when collected.</summary>
        public int ScoreValue => m_ScoreValue;

        /// <summary>Behavior of the obstacle during the run.</summary>
        public ObstacleBehaviour Behaviour => m_Behaviour;

        /// <summary>Whether the obstacle can be avoided by jumping.</summary>
        public bool CanBeJumped => m_CanBeJumped;

        /// <summary>Lateral movement speed for moving obstacles.</summary>
        public float MoveSpeed => m_MoveSpeed;

        #endregion
    }
}
