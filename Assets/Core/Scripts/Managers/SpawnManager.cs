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

        /// <summary>Fallback clearance behind a portal when nothing is configured.</summary>
        private const float c_DefaultPortalClearance = 40f;

        /// <summary>Fallback adaptation window when nothing is configured.</summary>
        private const float c_DefaultPortalCooldown = 25f;

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

        [Header("Portal Cooldown")]

        [SerializeField, Tooltip("Distance traveled after a portal before obstacles spawn again (adaptation window).")]
        private float m_PortalCooldown = 25f;

        [SerializeField, Tooltip("Minimum distance between an obstacle row and an active portal. Obstacles never spawn right next to a portal. When 0, the GameBalance value is used.")]
        private float m_PortalClearance = 0f;

        [SerializeField, Tooltip("Extra clearance applied ahead of the portal, because the obstacles that already spawned are still traveling toward it.")]
        private float m_PortalClearanceAhead = 0f;

        [SerializeField, Tooltip("Game balance asset with the portal clearance values. Used when the fields above are 0.")]
        private Config_GameBalance m_Balance;

        [SerializeField, Tooltip("Portal manager used to keep the obstacles away from the portals. Optional (found automatically when empty).")]
        private PortalManager m_PortalManager;

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
        private float m_DistanceAtLastPortal;

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

            // The obstacles are moved by the TrackManager with the same distance
            // as the tiles. Here we only decide when to spawn.
            if (m_IsSpawningEnabled)
            {
                TrySpawn(delta);
            }
        }

        /// <summary>
        /// Moves every active obstacle by the exact distance the track moved this
        /// frame. The distance comes from the caller so obstacles and tiles never
        /// drift apart.
        /// </summary>
        /// <param name="delta">Distance to move in world units.</param>
        public void MoveObstaclesWithTrack(float delta)
        {
            MoveObstacles(delta);
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
            m_DistanceAtLastPortal = -m_PortalCooldown;
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
        /// Opens the adaptation window: no obstacle spawns for the configured
        /// distance after a portal, giving the player time to get used to the new
        /// active color.
        /// </summary>
        public void OpenPortalCooldown()
        {
            m_DistanceAtLastPortal = m_RunManager.Distance;
            m_DistanceSinceLastSpawn = 0f;

            Debug.Log($"[SpawnManager] Portal cooldown opened for {GetPortalCooldown()} m.");
        }

        /// <summary>
        /// Returns the adaptation window: the Inspector value when set, otherwise
        /// the GameBalance value, otherwise the fallback.
        /// </summary>
        /// <returns>Cooldown in meters.</returns>
        private float GetPortalCooldown()
        {
            if (m_PortalCooldown > 0f)
            {
                return m_PortalCooldown;
            }

            if (m_Balance != null && m_Balance.PortalCooldown > 0f)
            {
                return m_Balance.PortalCooldown;
            }

            return c_DefaultPortalCooldown;
        }

        /// <summary>
        /// Removes every active obstacle too close to a world Z. Called when a
        /// portal spawns, so the player never meets a row of obstacles together
        /// with the portal. The clearance is asymmetric: ahead of the portal it
        /// is larger, because those obstacles are the ones the player would meet
        /// right after crossing.
        /// </summary>
        /// <param name="worldZ">World Z of the portal.</param>
        public void ClearObstaclesNear(float worldZ)
        {
            float clearanceBehind = GetClearanceBehind();
            float clearanceAhead = GetClearanceAhead();

            int removed = 0;

            for (int i = m_ActiveObstacles.Count - 1; i >= 0; i--)
            {
                ActiveObstacle active = m_ActiveObstacles[i];

                if (active.Instance == null)
                {
                    m_ActiveObstacles.RemoveAt(i);
                    continue;
                }

                float offset = active.Instance.transform.position.z - worldZ;
                float limit = offset >= 0f ? clearanceAhead : clearanceBehind;

                if (Mathf.Abs(offset) > limit)
                {
                    continue;
                }

                m_Pool.Release(active.Prefab, active.Instance);
                m_ActiveObstacles.RemoveAt(i);
                removed++;
            }

            if (removed > 0)
            {
                Debug.Log($"[SpawnManager] Cleared {removed} obstacle(s) near the portal at {worldZ:0} m.");
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

            // Adaptation window: right after a portal the player gets a clear
            // stretch of track to read the new color.
            if (m_RunManager.Distance - m_DistanceAtLastPortal < GetPortalCooldown())
            {
                return;
            }

            // Portal clearance: never spawn a row right next to a portal. The
            // obstacles spawn at the front edge of the track, so the check is
            // made against that exact position.
            if (!HasPortalClearance())
            {
                return;
            }

            m_DistanceSinceLastSpawn = 0f;
            SpawnObstacle();
        }

        /// <summary>
        /// Tells whether an obstacle can spawn at the front edge of the track,
        /// keeping the clearance distance from every active portal. The clearance
        /// is asymmetric: ahead of the portal it is larger, because the obstacles
        /// that already spawned are still traveling toward it and the player
        /// should meet them only after having read the new color.
        /// The distances are measured as the stretch the player still has to
        /// travel, which is the difference in world Z between the spawn point
        /// and the portal.
        /// </summary>
        /// <returns>True when an obstacle can spawn safely.</returns>
        private bool HasPortalClearance()
        {
            if (m_PortalManager == null)
            {
                m_PortalManager = FindFirstObjectByType<PortalManager>();
            }

            if (m_PortalManager == null)
            {
                return true;
            }

            float spawnZ = m_TrackManager.FrontZ;
            float clearanceBehind = GetClearanceBehind();
            float clearanceAhead = GetClearanceAhead();

            float nearestBehind = m_PortalManager.GetNearestPortalDistanceBehind(spawnZ);
            float nearestAhead = m_PortalManager.GetNearestPortalDistanceAhead(spawnZ);

            // The portals that already spawned sit BEHIND the spawn point: the
            // front edge of the track advances while they travel toward the camera.
            // An obstacle spawned now would reach the player after them, so the
            // distance to them is the "ahead" clearance.
            if (nearestBehind <= clearanceAhead)
            {
                Debug.Log($"[SpawnManager] Spawn blocked: portal {nearestBehind:0} m behind the spawn point ({spawnZ:0} m), needs {clearanceAhead:0} m.");
                return false;
            }

            // A portal spawned in the same frame can be slightly ahead of the
            // spawn point; that one needs the "behind" clearance.
            if (nearestAhead <= clearanceBehind)
            {
                Debug.Log($"[SpawnManager] Spawn blocked: portal {nearestAhead:0} m ahead of the spawn point ({spawnZ:0} m), needs {clearanceBehind:0} m.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the clearance applied behind the portal: the Inspector value
        /// when set, otherwise the GameBalance value.
        /// </summary>
        /// <returns>Clearance in meters.</returns>
        private float GetClearanceBehind()
        {
            if (m_PortalClearance > 0f)
            {
                return m_PortalClearance;
            }

            return m_Balance != null ? m_Balance.PortalClearance : c_DefaultPortalClearance;
        }

        /// <summary>
        /// Returns the clearance applied ahead of the portal: the base clearance
        /// plus the extra ahead value.
        /// </summary>
        /// <returns>Clearance in meters.</returns>
        private float GetClearanceAhead()
        {
            float extra = m_PortalClearanceAhead;

            if (extra <= 0f && m_Balance != null)
            {
                extra = m_Balance.PortalClearanceAhead;
            }

            return GetClearanceBehind() + extra;
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
