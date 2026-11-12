using System.Collections.Generic;
using UnityEngine;
using PowerPulse.Environment;
using PowerPulse.Obstacles;

namespace PowerPulse.Core
{
    /// <summary>
    /// Spawns and moves the obstacles of the current run. In Phase 5 it defines
    /// the first spawn pattern: one static obstacle per row, in a random lane,
    /// using only the two initial colors.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        // NOTE: TrackManager currently owns tile movement/recycling.
        // In a future refactor, merge tile management into this SpawnManager (per GDD).

        #region SERIALIZED FIELDS

        [Header("References")]
        [SerializeField, Tooltip("Run manager that provides distance and speed.")]
        private RunManager m_RunManager;

        [SerializeField, Tooltip("Shared object pool used to recycle obstacles.")]
        private ObjectPool m_Pool;

        [SerializeField, Tooltip("Track manager that provides the front edge of the track.")]
        private TrackManager m_TrackManager;

        [SerializeField, Tooltip("Catalog of obstacle shape prefabs.")]
        private SO_ObstacleCatalog m_Catalog;

        [Header("Pattern")]
        [SerializeField, Tooltip("Distance between obstacle rows, in meters.")]
        private float m_SpawnSpacing = 10f;

        [SerializeField, Tooltip("No obstacles spawn before this distance, in meters.")]
        private float m_InitialSafeDistance = 20f;

        [SerializeField, Tooltip("World Z below which an obstacle is recycled.")]
        private float m_RecycleZ = -15f;

        [SerializeField, Tooltip("Width of each lane in world units.")]
        private float m_LaneWidth = 1f;

        [SerializeField, Tooltip("Number of lanes on the track.")]
        private int m_LaneCount = 5;

        // TEMPORARY (Phase 5): the active colors are hardcoded here because the
        // ColorManager does not exist yet. In Phase 7/8 this list must be replaced
        // by the colors unlocked in the ColorManager.
        [SerializeField, Tooltip("TEMPORARY (Phase 5): colors used when spawning. Will come from the ColorManager in Phase 7/8.")]
        private List<ColorId> m_ActiveColors = new List<ColorId> { ColorId.Red, ColorId.Pink };

        [Header("Pooling")]
        [SerializeField, Tooltip("How many instances of each obstacle prefab are pre-created.")]
        private int m_ObstaclePrewarm = 10;

        #endregion


        #region NESTED TYPES

        /// <summary>An obstacle instance and the prefab it was created from.</summary>
        private class ActiveObstacle
        {
            public GameObject Prefab;
            public GameObject Instance;
        }

        #endregion


        #region FIELDS

        private readonly List<ActiveObstacle> m_ActiveObstacles = new List<ActiveObstacle>();
        private float m_DistanceSinceLastSpawn;

        #endregion


        #region UNITY CALLBACKS

        private void Start()
        {
            if (m_Pool == null || m_Catalog == null || m_TrackManager == null || m_RunManager == null)
            {
                Debug.LogError("[SpawnManager] Missing references. Assign the pool, the catalog, the track manager and the run manager.", this);
                enabled = false;
                return;
            }

            if (m_LaneCount <= 0)
            {
                Debug.LogError("[SpawnManager] Lane count must be greater than zero.", this);
                enabled = false;
                return;
            }

            if (m_ActiveColors.Count == 0)
            {
                Debug.LogError("[SpawnManager] At least one active color is required.", this);
                enabled = false;
                return;
            }

            PrewarmObstacles();
        }

        private void Update()
        {
            if (!m_RunManager.IsRunning)
            {
                return;
            }

            float delta = Time.deltaTime * m_RunManager.Speed;
            MoveObstacles(delta);
            TrySpawn(delta);
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Pre-creates the instances of every obstacle prefab in the catalog.
        /// </summary>
        private void PrewarmObstacles()
        {
            for (int i = 0; i < m_Catalog.ObstaclePrefabs.Count; i++)
            {
                GameObject prefab = m_Catalog.ObstaclePrefabs[i];
                if (prefab != null)
                {
                    m_Pool.CreatePool(prefab, m_ObstaclePrewarm);
                }
            }
        }

        /// <summary>
        /// Moves every active obstacle toward the camera and recycles the ones
        /// behind the recycle line.
        /// </summary>
        /// <param name="delta">Distance to move in world units.</param>
        private void MoveObstacles(float delta)
        {
            for (int i = m_ActiveObstacles.Count - 1; i >= 0; i--)
            {
                ActiveObstacle active = m_ActiveObstacles[i];
                active.Instance.transform.position += Vector3.back * delta;

                if (active.Instance.transform.position.z < m_RecycleZ)
                {
                    m_Pool.Release(active.Prefab, active.Instance);
                    m_ActiveObstacles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Spawns a new obstacle row when the spawn spacing and the initial safe
        /// distance are reached.
        /// </summary>
        /// <param name="delta">Distance moved this frame, in meters.</param>
        private void TrySpawn(float delta)
        {
            m_DistanceSinceLastSpawn += delta;

            if (m_DistanceSinceLastSpawn < m_SpawnSpacing)
            {
                return;
            }

            if (m_RunManager.Distance < m_InitialSafeDistance || m_Catalog.ObstaclePrefabs.Count == 0)
            {
                return;
            }

            m_DistanceSinceLastSpawn = 0f;
            SpawnObstacle();
        }

        /// <summary>
        /// Spawns one obstacle in a random lane, using a random prefab from the
        /// catalog and a random active color.
        /// </summary>
        private void SpawnObstacle()
        {
            int lane = Random.Range(0, m_LaneCount);
            float x = (lane - (m_LaneCount - 1) * 0.5f) * m_LaneWidth;

            GameObject prefab = m_Catalog.ObstaclePrefabs[Random.Range(0, m_Catalog.ObstaclePrefabs.Count)];

            if (prefab == null)
            {
                Debug.LogError("[SpawnManager] The catalog contains a null obstacle prefab.", this);
                return;
            }

            GameObject instance = m_Pool.Get(prefab, Vector3.zero, Quaternion.identity);
            Obstacle component = instance != null ? instance.GetComponent<Obstacle>() : null;

            if (component == null)
            {
                Debug.LogError("[SpawnManager] Every obstacle prefab needs an Obstacle component.", this);
                return;
            }

            instance.transform.position = new Vector3(x, component.Size.y * 0.5f, m_TrackManager.FrontZ);
            component.SetColor(m_ActiveColors[Random.Range(0, m_ActiveColors.Count)]);

            ActiveObstacle active = new ActiveObstacle { Prefab = prefab, Instance = instance };
            m_ActiveObstacles.Add(active);
        }

        #endregion
    }
}
