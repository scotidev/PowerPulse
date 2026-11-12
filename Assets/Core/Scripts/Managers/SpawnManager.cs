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

        #region STATIC

        /// <summary>Chance of a moving obstacle being the faster variant once both are enabled.</summary>
        private const float c_FasterChance = 0.35f;

        #endregion

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

        [Header("Colors")]

        [SerializeField, Tooltip("Score manager that owns the unlocked colors of the run.")]
        private ScoreManager m_ScoreManager;

        [Header("Movement")]

        [SerializeField, Tooltip("Lateral speed of Moving obstacles in units per second.")]
        private float m_MovingSpeed = 1.5f;

        [SerializeField, Tooltip("Lateral speed of Faster obstacles in units per second.")]
        private float m_FasterSpeed = 3f;

        [Header("Safety")]

        [SerializeField, Tooltip("Maximum number of obstacles spawned in the same row (safety rule: never block every lane).")]
        private int m_MaxObstaclesPerRow = 1;

        [Header("Lanes")]

        [SerializeField, Tooltip("Width of each lane in world units.")]
        private float m_LaneWidth = 1f;

        [SerializeField, Tooltip("Number of lanes on the track.")]
        private int m_LaneCount = 5;

        [SerializeField, Tooltip("TEMPORARY (Phase 5): colors used when the ScoreManager is not assigned. Replaced by the ScoreManager in Phase 6.")]
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
        private bool m_IsSpawningEnabled = true;

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

            if (m_ScoreManager != null)
            {
                m_ScoreManager.OnPlayerDied += HandlePlayerDied;
            }
        }

        private void OnDisable()
        {
            if (m_ScoreManager != null)
            {
                m_ScoreManager.OnPlayerDied -= HandlePlayerDied;
            }
        }

        private void Update()
        {
            if (!m_RunManager.IsRunning)
            {
                return;
            }

            float delta = Time.deltaTime * m_RunManager.Speed;
            MoveObstacles(delta);

            if (m_IsSpawningEnabled)
            {
                TrySpawn(delta);
            }
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Resets the spawn state for a new run: clears every active obstacle and
        /// the distance accumulator.
        /// </summary>
        public void ResetRun()
        {
            for (int i = 0; i < m_ActiveObstacles.Count; i++)
            {
                ActiveObstacle active = m_ActiveObstacles[i];
                if (active.Instance != null)
                {
                    m_Pool.Release(active.Prefab, active.Instance);
                }
            }

            m_ActiveObstacles.Clear();
            m_DistanceSinceLastSpawn = 0f;
            m_IsSpawningEnabled = true;
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

                MoveObstacleLaterally(active.Instance, delta);

                if (active.Instance.transform.position.z < m_RecycleZ)
                {
                    m_Pool.Release(active.Prefab, active.Instance);
                    m_ActiveObstacles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Moves a single obstacle laterally according to its behaviour. The
        /// SpawnManager stays the owner of the movement of every element.
        /// </summary>
        /// <param name="instance">Obstacle instance to move.</param>
        /// <param name="delta">Distance moved by the track this frame.</param>
        private void MoveObstacleLaterally(GameObject instance, float delta)
        {
            Obstacle component = instance.GetComponent<Obstacle>();

            if (component == null || !component.IsMovable)
            {
                return;
            }

            float speed = component.Behaviour == ObstacleBehaviour.Faster ? m_FasterSpeed : m_MovingSpeed;
            component.MoveLateral(speed * Time.deltaTime);
        }

        /// <summary>
        /// Chooses the behaviour of a newly spawned obstacle based on the
        /// milestones applied by the RunManager. Static until the first moving
        /// milestone; Faster only after the faster milestone.
        /// </summary>
        /// <returns>The behaviour to apply to the obstacle.</returns>
        private ObstacleBehaviour ChooseBehaviour()
        {
            if (m_RunManager.FasterEnabled && Random.value < c_FasterChance)
            {
                return ObstacleBehaviour.Faster;
            }

            if (m_RunManager.MovingEnabled)
            {
                return ObstacleBehaviour.Moving;
            }

            return ObstacleBehaviour.Static;
        }

        /// <summary>
        /// Handles the player death: stops spawning new obstacles and lets the
        /// existing ones keep moving so the death is readable.
        /// </summary>
        private void HandlePlayerDied()
        {
            m_IsSpawningEnabled = false;
        }

        /// <summary>
        /// Enables or disables the spawning of new obstacles. Used when the run
        /// starts again so the death handler does not permanently disable the
        /// component.
        /// </summary>
        /// <param name="enabled">True to allow spawning.</param>
        public void SetSpawningEnabled(bool enabled)
        {
            m_IsSpawningEnabled = enabled;
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
            int obstacleCount = Mathf.Clamp(m_RunManager.MaxObstaclesPerRow, 1, m_MaxObstaclesPerRow);
            int[] lanes = PickFreeLanes(obstacleCount);

            for (int i = 0; i < lanes.Length; i++)
            {
                SpawnSingleObstacle(lanes[i]);
            }
        }

        /// <summary>
        /// Picks random lanes for the obstacles of this row, keeping at least one
        /// lane free so the player always has a way through (safety rule).
        /// </summary>
        /// <param name="count">How many obstacles the row needs.</param>
        /// <returns>The lanes chosen for the obstacles.</returns>
        private int[] PickFreeLanes(int count)
        {
            int[] lanes = new int[m_LaneCount];

            for (int i = 0; i < m_LaneCount; i++)
            {
                lanes[i] = i;
            }

            // Fisher-Yates shuffle.
            for (int i = lanes.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = lanes[i];
                lanes[i] = lanes[j];
                lanes[j] = temp;
            }

            // One lane is deliberately left unused, so the row is always passable.
            int obstacleCount = Mathf.Min(count, m_LaneCount - 1);

            int[] result = new int[obstacleCount];

            for (int i = 0; i < obstacleCount; i++)
            {
                result[i] = lanes[i];
            }

            return result;
        }

        /// <summary>
        /// Spawns a single obstacle in the given lane, using a random prefab from
        /// the catalog, a random color in play and the behaviour allowed by the
        /// current milestone.
        /// </summary>
        /// <param name="lane">Lane index (0 to LaneCount - 1).</param>
        private void SpawnSingleObstacle(int lane)
        {
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
            component.SourcePrefab = prefab;
            component.SetColor(GetRandomSpawnColor());

            ObstacleBehaviour behaviour = ChooseBehaviour();
            component.SetBehaviour(behaviour, GetBehaviourSpeed(behaviour));

            ActiveObstacle active = new ActiveObstacle { Prefab = prefab, Instance = instance };
            m_ActiveObstacles.Add(active);
        }

        /// <summary>
        /// Returns a random color among the colors currently in play. The pool is
        /// the active color plus every unlocked color plus every boost color —
        /// that is exactly the variety the player is trained to read, and it is
        /// always a fair death (the color was announced by the portal/boost).
        /// Falls back to the temporary list when the ScoreManager is not assigned.
        /// </summary>
        /// <returns>A color to paint the spawned obstacle.</returns>
        private ColorId GetRandomSpawnColor()
        {
            if (m_ScoreManager != null)
            {
                List<ColorId> pool = new List<ColorId> { m_ScoreManager.ActiveColor };

                List<ColorId> unlocked = m_ScoreManager.UnlockedColors;

                if (unlocked != null && unlocked.Count > 0)
                {
                    pool.AddRange(unlocked);
                }

                List<ColorId> boost = m_ScoreManager.BoostColors;

                if (boost != null && boost.Count > 0)
                {
                    pool.AddRange(boost);
                }

                return pool[Random.Range(0, pool.Count)];
            }

            if (m_ActiveColors.Count == 0)
            {
                Debug.LogError("[SpawnManager] No color is available to spawn obstacles.", this);
                return ColorId.Red;
            }

            return m_ActiveColors[Random.Range(0, m_ActiveColors.Count)];
        }

        /// <summary>
        /// Returns the lateral speed for the given behaviour.
        /// </summary>
        /// <param name="behaviour">Behaviour of the obstacle.</param>
        /// <returns>Lateral speed in units per second.</returns>
        private float GetBehaviourSpeed(ObstacleBehaviour behaviour)
        {
            switch (behaviour)
            {
                case ObstacleBehaviour.Faster:
                    return m_FasterSpeed;

                case ObstacleBehaviour.Moving:
                    return m_MovingSpeed;

                default:
                    return 0f;
            }
        }

        #endregion
    }
}
