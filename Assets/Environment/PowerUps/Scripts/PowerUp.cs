using UnityEngine;
using PowerPulse.Core;

namespace PowerPulse.PowerUps
{
    /// <summary>
    /// A collectible power-up on the track. It detects the player and notifies the
    /// SpawnManager, which owns the effect. The visual is authored on the prefab;
    /// this component only adds the idle animation and the collection detection.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PowerUp : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("Animation")]

        [SerializeField, Tooltip("Rotation speed of the visual in degrees per second (0 = no rotation).")]
        private float m_RotationSpeed = 90f;

        [SerializeField, Tooltip("Floating amplitude in units (0 = no float).")]
        private float m_FloatAmplitude = 0.15f;

        [SerializeField, Tooltip("Floating speed in cycles per second.")]
        private float m_FloatSpeed = 2f;

        #endregion


        #region FIELDS

        private SpawnManager m_Manager;
        private SO_PowerUpCatalog.PowerUpEntry m_Entry;
        private Vector3 m_BasePosition;
        private float m_AnimationTime;

        #endregion


        #region PROPERTIES

        /// <summary>Data of this power-up, defined in the catalog.</summary>
        public SO_PowerUpCatalog.PowerUpEntry Entry => m_Entry;

        /// <summary>Prefab the instance was pooled from. Stored at spawn so the instance can be recycled.</summary>
        public GameObject SourcePrefab { get; set; }

        #endregion


        #region UNITY CALLBACKS

        private void Update()
        {
            m_AnimationTime += Time.deltaTime;
            Animate();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            m_Manager?.HandlePowerUpCollected(this);
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Configures the power-up for a new spawn.
        /// </summary>
        /// <param name="entry">Catalog entry with the power-up data.</param>
        /// <param name="manager">Manager that owns the collection rule.</param>
        public void SetPowerUp(SO_PowerUpCatalog.PowerUpEntry entry, SpawnManager manager)
        {
            m_Entry = entry;
            m_Manager = manager;
            m_BasePosition = transform.position;
            m_AnimationTime = 0f;
        }

        /// <summary>
        /// Resets the power-up so it can be reused from the pool.
        /// </summary>
        public void ResetPowerUp()
        {
            m_Entry = null;
            m_Manager = null;
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Applies the idle animation (rotation and float) without changing the
        /// X/Z position owned by the SpawnManager.
        /// </summary>
        private void Animate()
        {
            if (m_RotationSpeed != 0f)
            {
                transform.Rotate(Vector3.up, m_RotationSpeed * Time.deltaTime);
            }

            if (m_FloatAmplitude > 0f && m_FloatSpeed > 0f)
            {
                float offset = Mathf.Sin(m_AnimationTime * m_FloatSpeed * Mathf.PI * 2f) * m_FloatAmplitude;
                transform.position = new Vector3(transform.position.x, m_BasePosition.y + offset, transform.position.z);
            }
        }

        #endregion
    }
}
