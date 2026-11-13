using System.Collections.Generic;
using UnityEngine;
using PowerPulse.Environment;
using PowerPulse.Obstacles;
using PowerPulse.PowerUps;

namespace PowerPulse.Core
{
    /// <summary>
    /// Owns the spawning and the movement of everything the track carries:
    /// obstacles, color portals and power-ups. The TrackManager moves the tiles
    /// and calls <see cref="MoveSceneWithTrack"/> so every element uses the same
    /// delta and never drifts apart. The ScoreManager owns the run state and the
    /// power-up effects.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        #region STATIC

        /// <summary>Chance of a moving obstacle being the faster variant once both are enabled.</summary>
        private const float c_FasterChance = 0.35f;

        /// <summary>Fallback clearance behind a portal when nothing is configured.</summary>
        private const float c_DefaultPortalClearance = 40f;

        /// <summary>Fallback adaptation window when nothing is configured.</summary>
        private const float c_DefaultPortalCooldown = 25f;

        /// <summary>Fallback portal interval when nothing is configured.</summary>
        private const float c_DefaultPortalInterval = 500f;

        /// <summary>Fallback power-up interval when nothing is configured.</summary>
        private const float c_DefaultPowerUpInterval = 250f;

        #endregion


        #region SERIALIZED FIELDS

        [Header("References")]
        [SerializeField, Tooltip("Run manager that provides distance and speed.")]
        private RunManager m_RunManager;

        [SerializeField, Tooltip("Shared object pool used to recycle every spawned element.")]
        private ObjectPool m_Pool;

        [SerializeField, Tooltip("Track manager that provides the front edge of the track.")]
        private TrackManager m_TrackManager;

        [SerializeField, Tooltip("Catalog of obstacle shape prefabs.")]
        private SO_ObstacleCatalog m_Catalog;

        [SerializeField, Tooltip("Color palette with the five official colors and the unlock order.")]
        private SO_ColorPalette m_Palette;

        [SerializeField, Tooltip("Score manager that owns the run state and the power-up effects.")]
        private ScoreManager m_ScoreManager;

        [Header("Obstacles")]
        [SerializeField, Tooltip("Distance between obstacle rows, in meters.")]
        private float m_SpawnSpacing = 10f;

        [SerializeField, Tooltip("No obstacles spawn before this distance, in meters.")]
        private float m_InitialSafeDistance = 20f;

        [SerializeField, Tooltip("World Z below which an obstacle is recycled.")]
        private float m_RecycleZ = -15f;

        [Header("Portals")]
        [SerializeField, Tooltip("Portal prefab.")]
        private GameObject m_PortalPrefab;

        [SerializeField, Tooltip("No portal spawns before this distance, in meters.")]
        private float m_PortalInitialSafeDistance = 500f;

        [SerializeField, Tooltip("Distance between portals in meters. Uses the GameBalance value by default (0 = use config).")]
        private float m_PortalInterval = 0f;

        [SerializeField, Tooltip("World Z below which a portal is recycled.")]
        private float m_PortalRecycleZ = -15f;

        [SerializeField, Tooltip("How many portal instances are pre-created.")]
        private int m_PortalPrewarm = 2;

        [Header("Power-ups")]
        [SerializeField, Tooltip("Catalog of power-up definitions (prefab + data).")]
        private SO_PowerUpCatalog m_PowerUpCatalog;

        [SerializeField, Tooltip("Average distance between power-up spawns in meters. Uses the GameBalance value by default (0 = use config).")]
        private float m_PowerUpInterval = 0f;

        [SerializeField, Tooltip("Minimum distance between power-up spawns in meters (safety rule against excessive sequences).")]
        private float m_MinPowerUpInterval = 100f;

        [SerializeField, Tooltip("Height of the power-up above the track in world units.")]
        private float m_PowerUpHeight = 1f;

        [SerializeField, Tooltip("Minimum distance between a power-up and any obstacle when spawning.")]
        private float m_PowerUpObstacleClearance = 5f;

        [SerializeField, Tooltip("Minimum distance between a power-up and any active portal. Prevents a boost from appearing right before a portal.")]
        private float m_PowerUpPortalClearance = 30f;

        [SerializeField, Tooltip("How many instances of each power-up prefab are pre-created.")]
        private int m_PowerUpPrewarm = 6;

        [Header("Portal Cooldown")]
        [SerializeField, Tooltip("Distance traveled after a portal before obstacles spawn again (adaptation window).")]
        private float m_PortalCooldown = 25f;

        [SerializeField, Tooltip("Minimum distance between an obstacle row and an active portal. Obstacles never spawn right next to a portal. When 0, the GameBalance value is used.")]
        private float m_PortalClearance = 0f;

        [SerializeField, Tooltip("Extra clearance applied ahead of the portal, because the obstacles that already spawned are still traveling toward it.")]
        private float m_PortalClearanceAhead = 0f;

        [SerializeField, Tooltip("Game balance asset with the portal clearance values. Used when the fields above are 0.")]
        private Config_GameBalance m_Balance;

        [Header("Colors")]
        [SerializeField, Tooltip("TEMPORARY (Phase 5): colors used when the ScoreManager is not assigned. Replaced by the ScoreManager in Phase 6.")]
        private List<ColorId> m_ActiveColors = new List<ColorId> { ColorId.Red, ColorId.Pink };

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


        /// <summary>A power-up instance and the prefab it was created from.</summary>
        private class ActivePowerUp
        {
            public GameObject Prefab;
            public GameObject Instance;
        }

        #endregion


        #region FIELDS

        private readonly List<ActiveObstacle> m_ActiveObstacles = new List<ActiveObstacle>();
        private readonly List<GameObject> m_ActivePortals = new List<GameObject>();
        private readonly List<ActivePowerUp> m_ActivePowerUps = new List<ActivePowerUp>();

        private float m_DistanceSinceLastSpawn;
        private bool m_IsSpawningEnabled = true;
        private float m_DistanceAtLastPortal;

        private float m_DistanceSinceLastPortal;
        private int m_NextColorIndex;

        private float m_DistanceSinceLastPowerUp;

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

            if (m_PortalPrefab == null)
            {
                Debug.LogWarning("[SpawnManager] The portal prefab is not assigned. Portals will not spawn.", this);
            }
            else if (m_PortalPrefab.GetComponent<Portal>() == null)
            {
                Debug.LogError("[SpawnManager] The portal prefab needs a Portal component. Portals will not spawn.", this);
                m_PortalPrefab = null;
            }

            PrewarmObstacles();
            PrewarmPortals();
            PrewarmPowerUps();

            m_NextColorIndex = m_ScoreManager != null ? m_ScoreManager.UnlockedColors.Count : 0;

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
            if (!m_RunManager.IsRunning || !m_IsSpawningEnabled)
            {
                return;
            }

            float delta = Time.deltaTime * m_RunManager.Speed;

            TrySpawnObstacles(delta);
            TrySpawnPortal(delta);
            TrySpawnPowerUp(delta);
        }

        /// <summary>
        /// Moves every active element by the exact distance the track moved this
        /// frame. The distance comes from the caller so tiles, obstacles, portals
        /// and power-ups never drift apart.
        /// </summary>
        /// <param name="delta">Distance to move in world units.</param>
        public void MoveSceneWithTrack(float delta)
        {
            MoveObstacles(delta);
            MovePortals(delta);
            MovePowerUps(delta);
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Resets the spawn state for a new run: clears every active element and
        /// the distance accumulators.
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

            ResetPortals();
            ResetPowerUps();

            m_IsSpawningEnabled = true;
            m_DistanceAtLastPortal = -m_PortalCooldown;
        }

        /// <summary>
        /// Enables or disables the spawning of new elements. Used when the run
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
        /// Returns the clearance applied behind the portal, so other systems can
        /// keep the same distance.
        /// </summary>
        /// <returns>Clearance in meters.</returns>
        public float GetPortalClearanceBehind()
        {
            return GetClearanceBehind();
        }

        /// <summary>
        /// Returns the clearance applied ahead of the portal, so other systems can
        /// keep the same distance.
        /// </summary>
        /// <returns>Clearance in meters.</returns>
        public float GetPortalClearanceAhead()
        {
            return GetClearanceAhead();
        }

        /// <summary>
        /// Finds a world Z at least the given clearance away from every active
        /// obstacle, starting from the desired Z and moving forward when needed.
        /// </summary>
        /// <param name="desiredZ">Preferred world Z.</param>
        /// <param name="clearance">Minimum distance from any obstacle.</param>
        /// <returns>A world Z far enough from every obstacle.</returns>
        public float FindSafeSpawnZ(float desiredZ, float clearance)
        {
            if (clearance <= 0f)
            {
                return desiredZ;
            }

            float safeZ = desiredZ;

            for (int i = 0; i < m_ActiveObstacles.Count; i++)
            {
                ActiveObstacle active = m_ActiveObstacles[i];

                if (active.Instance == null)
                {
                    continue;
                }

                float obstacleZ = active.Instance.transform.position.z;

                if (Mathf.Abs(obstacleZ - safeZ) >= clearance)
                {
                    continue;
                }

                safeZ = obstacleZ + clearance;
            }

            return safeZ;
        }

        /// <summary>
        /// Applies the color change when the player crosses a portal: the portal
        /// becomes the active color and the next color in the unlock order is
        /// unlocked.
        /// </summary>
        /// <param name="portal">Portal the player crossed.</param>
        public void HandlePortalCrossed(Portal portal)
        {
            if (portal == null || m_ScoreManager == null)
            {
                return;
            }

            ColorId newColor = portal.TargetColor;
            m_ScoreManager.SetActiveColor(newColor);
            ColorId unlocked = m_ScoreManager.UnlockNextColor();

            OpenPortalCooldown();

            Debug.Log($"[SpawnManager] Player crossed. New active color: {newColor} | Unlocked: {unlocked}");
        }

        /// <summary>
        /// Returns the distance to the nearest active portal behind the given
        /// world Z (closer to the camera). Returns a large value when there is none.
        /// </summary>
        /// <param name="worldZ">World Z to measure from.</param>
        /// <returns>Distance to the nearest portal behind.</returns>
        public float GetNearestPortalDistanceBehind(float worldZ)
        {
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < m_ActivePortals.Count; i++)
            {
                GameObject portal = m_ActivePortals[i];

                if (portal == null)
                {
                    continue;
                }

                float distance = worldZ - portal.transform.position.z;

                if (distance < 0f)
                {
                    continue;
                }

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return nearestDistance;
        }

        /// <summary>
        /// Returns the distance to the nearest active portal ahead of the given
        /// world Z (further from the camera). Returns a large value when there is none.
        /// </summary>
        /// <param name="worldZ">World Z to measure from.</param>
        /// <returns>Distance to the nearest portal ahead.</returns>
        public float GetNearestPortalDistanceAhead(float worldZ)
        {
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < m_ActivePortals.Count; i++)
            {
                GameObject portal = m_ActivePortals[i];

                if (portal == null)
                {
                    continue;
                }

                float distance = portal.transform.position.z - worldZ;

                if (distance < 0f)
                {
                    continue;
                }

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return nearestDistance;
        }

        /// <summary>
        /// Handles the collection of a power-up: applies the effect to the
        /// ScoreManager and recycles the instance.
        /// </summary>
        /// <param name="powerUp">Power-up collected.</param>
        public void HandlePowerUpCollected(PowerUp powerUp)
        {
            if (powerUp == null || m_ScoreManager == null)
            {
                return;
            }

            SO_PowerUpCatalog.PowerUpEntry entry = powerUp.Entry;

            if (entry == null)
            {
                return;
            }

            ApplyPowerUpEffect(entry);

            GameObject prefab = powerUp.SourcePrefab != null ? powerUp.SourcePrefab : powerUp.gameObject;
            m_Pool.Release(prefab, powerUp.gameObject);

            for (int i = m_ActivePowerUps.Count - 1; i >= 0; i--)
            {
                if (m_ActivePowerUps[i].Instance == powerUp.gameObject)
                {
                    m_ActivePowerUps.RemoveAt(i);
                    break;
                }
            }
        }

        #endregion


        #region PRIVATE METHODS

        // ===== OBSTACLES =====

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
        /// Moves a single obstacle laterally according to its behaviour.
        /// </summary>
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
        /// Spawns a new obstacle row when the spawn spacing and the initial safe
        /// distance are reached.
        /// </summary>
        private void TrySpawnObstacles(float delta)
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

            if (m_RunManager.Distance - m_DistanceAtLastPortal < GetPortalCooldown())
            {
                return;
            }

            if (!HasPortalClearance())
            {
                return;
            }

            m_DistanceSinceLastSpawn = 0f;
            SpawnObstacle();
        }

        /// <summary>
        /// Chooses the behaviour of a newly spawned obstacle based on the milestones.
        /// </summary>
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
        /// Tells whether an obstacle can spawn at the front edge of the track,
        /// keeping the clearance distance from every active portal.
        /// </summary>
        private bool HasPortalClearance()
        {
            float spawnZ = m_TrackManager.FrontZ;
            float clearanceBehind = GetClearanceBehind();
            float clearanceAhead = GetClearanceAhead();

            float nearestBehind = GetNearestPortalDistanceBehind(spawnZ);
            float nearestAhead = GetNearestPortalDistanceAhead(spawnZ);

            if (nearestBehind <= clearanceAhead)
            {
                Debug.Log($"[SpawnManager] Spawn blocked: portal {nearestBehind:0} m behind the spawn point ({spawnZ:0} m), needs {clearanceAhead:0} m.");
                return false;
            }

            if (nearestAhead <= clearanceBehind)
            {
                Debug.Log($"[SpawnManager] Spawn blocked: portal {nearestAhead:0} m ahead of the spawn point ({spawnZ:0} m), needs {clearanceBehind:0} m.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Spawns one obstacle row, keeping at least one lane free.
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
        /// lane free so the player always has a way through.
        /// </summary>
        private int[] PickFreeLanes(int count)
        {
            int[] lanes = new int[m_LaneCount];

            for (int i = 0; i < m_LaneCount; i++)
            {
                lanes[i] = i;
            }

            for (int i = lanes.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = lanes[i];
                lanes[i] = lanes[j];
                lanes[j] = temp;
            }

            int obstacleCount = Mathf.Min(count, m_LaneCount - 1);
            int[] result = new int[obstacleCount];

            for (int i = 0; i < obstacleCount; i++)
            {
                result[i] = lanes[i];
            }

            return result;
        }

        /// <summary>
        /// Spawns a single obstacle in the given lane.
        /// </summary>
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
        /// Returns a random color among the colors currently in play.
        /// </summary>
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

        /// <summary>
        /// Handles the player death: stops spawning new elements.
        /// </summary>
        private void HandlePlayerDied()
        {
            m_IsSpawningEnabled = false;
        }

        /// <summary>
        /// Returns the adaptation window: the Inspector value when set, otherwise
        /// the GameBalance value, otherwise the fallback.
        /// </summary>
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
        /// Returns the clearance applied behind the portal.
        /// </summary>
        private float GetClearanceBehind()
        {
            if (m_PortalClearance > 0f)
            {
                return m_PortalClearance;
            }

            return m_Balance != null ? m_Balance.PortalClearance : c_DefaultPortalClearance;
        }

        /// <summary>
        /// Returns the clearance applied ahead of the portal.
        /// </summary>
        private float GetClearanceAhead()
        {
            float extra = m_PortalClearanceAhead;

            if (extra <= 0f && m_Balance != null)
            {
                extra = m_Balance.PortalClearanceAhead;
            }

            return GetClearanceBehind() + extra;
        }

        // ===== PORTALS =====

        /// <summary>
        /// Pre-creates the portal instances.
        /// </summary>
        private void PrewarmPortals()
        {
            if (m_PortalPrefab == null)
            {
                return;
            }

            m_Pool.CreatePool(m_PortalPrefab, m_PortalPrewarm);
        }

        /// <summary>
        /// Moves every active portal toward the camera and recycles the ones behind
        /// the recycle line.
        /// </summary>
        private void MovePortals(float delta)
        {
            if (m_PortalPrefab == null)
            {
                return;
            }

            for (int i = m_ActivePortals.Count - 1; i >= 0; i--)
            {
                GameObject portal = m_ActivePortals[i];
                portal.transform.position += Vector3.back * delta;

                if (portal.transform.position.z < m_PortalRecycleZ)
                {
                    m_Pool.Release(m_PortalPrefab, portal);
                    m_ActivePortals.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Spawns a new portal when the configured interval is reached.
        /// </summary>
        private void TrySpawnPortal(float delta)
        {
            if (m_PortalPrefab == null)
            {
                return;
            }

            m_DistanceSinceLastPortal += delta;

            if (m_DistanceSinceLastPortal < m_PortalInitialSafeDistance)
            {
                return;
            }

            float interval = m_PortalInterval;

            if (interval <= 0f && m_Balance != null)
            {
                interval = m_Balance.PortalInterval;
            }

            if (interval <= 0f)
            {
                interval = c_DefaultPortalInterval;
            }

            if (m_DistanceSinceLastPortal < interval)
            {
                return;
            }

            m_DistanceSinceLastPortal = 0f;
            SpawnPortal();
        }

        /// <summary>
        /// Spawns a portal at a safe world Z, far enough from every active obstacle.
        /// </summary>
        private void SpawnPortal()
        {
            if (m_PortalPrefab == null || m_ActivePortals.Count >= m_PortalPrewarm)
            {
                return;
            }

            GameObject instance = m_Pool.Get(m_PortalPrefab, Vector3.zero, Quaternion.identity);

            if (instance == null)
            {
                return;
            }

            Portal portal = instance.GetComponent<Portal>();
            portal.SetPortal(GetNextPortalColor(), this);

            instance.transform.position = new Vector3(0f, 0f, GetSafePortalZ());
            m_ActivePortals.Add(instance);

            Debug.Log($"[SpawnManager] Portal spawned at {m_RunManager.Distance:0} m with color {portal.TargetColor}.");
        }

        /// <summary>
        /// Returns a world Z far enough from every active obstacle.
        /// </summary>
        private float GetSafePortalZ()
        {
            float desiredZ = m_TrackManager.FrontZ;
            float clearance = GetClearanceBehind();

            if (clearance <= 0f)
            {
                return desiredZ;
            }

            float safeZ = FindSafeSpawnZ(desiredZ, clearance);

            return Mathf.Max(desiredZ, safeZ);
        }

        /// <summary>
        /// Returns the next color in the unlock order, or a random unlocked color
        /// when every color is already unlocked.
        /// </summary>
        private ColorId GetNextPortalColor()
        {
            List<ColorId> order = m_Palette != null ? m_Palette.GetUnlockOrder() : null;

            if (order == null || order.Count == 0)
            {
                return ColorId.Red;
            }

            if (m_NextColorIndex < order.Count)
            {
                ColorId color = order[m_NextColorIndex];
                m_NextColorIndex++;
                return color;
            }

            List<ColorId> unlocked = m_ScoreManager.UnlockedColors;

            if (unlocked == null || unlocked.Count == 0)
            {
                return order[0];
            }

            return unlocked[Random.Range(0, unlocked.Count)];
        }

        /// <summary>
        /// Clears the active portals for a new run.
        /// </summary>
        private void ResetPortals()
        {
            for (int i = 0; i < m_ActivePortals.Count; i++)
            {
                if (m_ActivePortals[i] != null)
                {
                    m_Pool.Release(m_PortalPrefab, m_ActivePortals[i]);
                }
            }

            m_ActivePortals.Clear();
            m_DistanceSinceLastPortal = 0f;
            m_NextColorIndex = m_ScoreManager != null ? m_ScoreManager.UnlockedColors.Count : 0;
        }

        // ===== POWER-UPS =====

        /// <summary>
        /// Pre-creates the instances of every power-up prefab in the catalog.
        /// </summary>
        private void PrewarmPowerUps()
        {
            if (m_PowerUpCatalog == null)
            {
                return;
            }

            for (int i = 0; i < m_PowerUpCatalog.PowerUps.Count; i++)
            {
                SO_PowerUpCatalog.PowerUpEntry entry = m_PowerUpCatalog.PowerUps[i];

                if (entry != null && entry.Prefab != null)
                {
                    m_Pool.CreatePool(entry.Prefab, m_PowerUpPrewarm);
                }
            }
        }

        /// <summary>
        /// Moves every active power-up toward the camera and recycles the ones
        /// behind the recycle line.
        /// </summary>
        private void MovePowerUps(float delta)
        {
            for (int i = m_ActivePowerUps.Count - 1; i >= 0; i--)
            {
                ActivePowerUp active = m_ActivePowerUps[i];
                active.Instance.transform.position += Vector3.back * delta;

                if (active.Instance.transform.position.z < m_RecycleZ)
                {
                    m_Pool.Release(active.Prefab, active.Instance);
                    m_ActivePowerUps.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Spawns a new power-up when the interval, the minimum sequence, the safe
        /// distance and the portal adaptation window are reached.
        /// </summary>
        private void TrySpawnPowerUp(float delta)
        {
            m_DistanceSinceLastPowerUp += delta;

            float interval = m_PowerUpInterval;

            if (interval <= 0f && m_Balance != null)
            {
                interval = m_Balance.PowerUpInterval;
            }

            if (interval <= 0f)
            {
                interval = c_DefaultPowerUpInterval;
            }

            if (m_DistanceSinceLastPowerUp < interval)
            {
                return;
            }

            if (m_DistanceSinceLastPowerUp < m_MinPowerUpInterval)
            {
                return;
            }

            if (m_RunManager.Distance < m_InitialSafeDistance || m_PowerUpCatalog == null || m_PowerUpCatalog.PowerUps.Count == 0)
            {
                return;
            }

            if (!HasAvailablePowerUp())
            {
                return;
            }

            if (m_RunManager.Distance - m_DistanceAtLastPortal < GetPortalCooldown())
            {
                return;
            }

            float safeZ = FindSafeSpawnZ(m_TrackManager.FrontZ, m_PowerUpObstacleClearance);

            if (!HasPowerUpPortalClearance(safeZ))
            {
                return;
            }

            m_DistanceSinceLastPowerUp = 0f;
            SpawnPowerUp(safeZ);
        }

        /// <summary>
        /// Spawns one power-up in a random lane, at the given safe Z.
        /// </summary>
        /// <param name="safeZ">World Z already cleared from obstacles and portals.</param>
        private void SpawnPowerUp(float safeZ)
        {
            int lane = Random.Range(0, m_LaneCount);
            float x = (lane - (m_LaneCount - 1) * 0.5f) * m_LaneWidth;

            List<SO_PowerUpCatalog.PowerUpEntry> available = GetAvailablePowerUps();

            if (available.Count == 0)
            {
                return;
            }

            SO_PowerUpCatalog.PowerUpEntry entry = available[Random.Range(0, available.Count)];

            if (entry == null || entry.Prefab == null)
            {
                Debug.LogError("[SpawnManager] The catalog contains a null power-up entry or prefab.", this);
                return;
            }

            GameObject instance = m_Pool.Get(entry.Prefab, new Vector3(x, m_PowerUpHeight, safeZ), Quaternion.identity);
            PowerUp powerUp = instance != null ? instance.GetComponent<PowerUp>() : null;

            if (powerUp == null)
            {
                Debug.LogError("[SpawnManager] Every power-up prefab needs a PowerUp component.", this);
                return;
            }

            powerUp.SourcePrefab = entry.Prefab;
            powerUp.SetPowerUp(entry, this);

            ActivePowerUp active = new ActivePowerUp { Prefab = entry.Prefab, Instance = instance };
            m_ActivePowerUps.Add(active);

            Debug.Log($"[SpawnManager] Power-up spawned: {entry.Type} at {m_RunManager.Distance:0} m.");
        }

        /// <summary>
        /// Tells whether at least one power-up in the catalog can spawn right now.
        /// </summary>
        /// <returns>True when there is an available power-up.</returns>
        private bool HasAvailablePowerUp()
        {
            if (m_PowerUpCatalog == null)
            {
                return false;
            }

            for (int i = 0; i < m_PowerUpCatalog.PowerUps.Count; i++)
            {
                if (IsPowerUpAvailable(m_PowerUpCatalog.PowerUps[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns every catalog entry that can spawn right now.
        /// </summary>
        /// <returns>The available power-up entries.</returns>
        private List<SO_PowerUpCatalog.PowerUpEntry> GetAvailablePowerUps()
        {
            List<SO_PowerUpCatalog.PowerUpEntry> available = new List<SO_PowerUpCatalog.PowerUpEntry>();

            if (m_PowerUpCatalog == null)
            {
                return available;
            }

            for (int i = 0; i < m_PowerUpCatalog.PowerUps.Count; i++)
            {
                SO_PowerUpCatalog.PowerUpEntry entry = m_PowerUpCatalog.PowerUps[i];

                if (entry != null && entry.Prefab != null && IsPowerUpAvailable(entry))
                {
                    available.Add(entry);
                }
            }

            return available;
        }

        /// <summary>
        /// Tells whether a power-up can spawn with the colors currently unlocked.
        /// The color boosts only appear once the colors they cover are available.
        /// </summary>
        /// <param name="entry">Power-up entry to check.</param>
        /// <returns>True when the power-up is available.</returns>
        private bool IsPowerUpAvailable(SO_PowerUpCatalog.PowerUpEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            if (entry.Type == PowerUpType.Shield)
            {
                return true;
            }

            int unlockedCount = m_ScoreManager != null ? m_ScoreManager.UnlockedColors.Count : 0;

            switch (entry.Type)
            {
                case PowerUpType.BoostTwoColors:
                    return unlockedCount >= 2;

                case PowerUpType.BoostThreeColors:
                    return unlockedCount >= 3;

                case PowerUpType.BoostAllColors:
                    int totalColors = m_Palette != null ? m_Palette.ColorCount : 5;
                    return unlockedCount >= totalColors;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Tells whether a power-up can spawn at the given world Z without being
        /// too close to an active portal. This prevents a boost from appearing
        /// right before a portal.
        /// </summary>
        /// <param name="powerUpZ">World Z of the power-up.</param>
        /// <returns>True when the position is clear of portals.</returns>
        private bool HasPowerUpPortalClearance(float powerUpZ)
        {
            if (m_PowerUpPortalClearance <= 0f)
            {
                return true;
            }

            if (GetNearestPortalDistanceBehind(powerUpZ) <= m_PowerUpPortalClearance)
            {
                return false;
            }

            if (GetNearestPortalDistanceAhead(powerUpZ) <= m_PowerUpPortalClearance)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Applies the power-up effect to the ScoreManager.
        /// </summary>
        private void ApplyPowerUpEffect(SO_PowerUpCatalog.PowerUpEntry entry)
        {
            switch (entry.Type)
            {
                case PowerUpType.Shield:
                    if (!m_ScoreManager.HasShield)
                    {
                        m_ScoreManager.AddShield();
                        Debug.Log("[SpawnManager] Shield granted.");
                    }
                    else
                    {
                        Debug.Log("[SpawnManager] Shield already active: ignored.");
                    }
                    break;

                case PowerUpType.BoostTwoColors:
                case PowerUpType.BoostThreeColors:
                case PowerUpType.BoostAllColors:
                    List<ColorId> colors = ResolveBoostColors(entry);
                    m_ScoreManager.ActivateBoost(colors, entry.Duration);
                    Debug.Log($"[SpawnManager] Boost granted: {string.Join(", ", colors)} for {entry.Duration:0.#} s.");
                    break;
            }
        }

        /// <summary>
        /// Resolves the colors covered by a boost. The boost always covers the
        /// active color plus other unlocked colors (or every color for the
        /// all-colors boost). The catalog list is used as a fallback when there
        /// are not enough unlocked colors.
        /// </summary>
        private List<ColorId> ResolveBoostColors(SO_PowerUpCatalog.PowerUpEntry entry)
        {
            List<ColorId> colors = new List<ColorId>();

            if (entry.Type == PowerUpType.BoostAllColors)
            {
                foreach (ColorId color in System.Enum.GetValues(typeof(ColorId)))
                {
                    colors.Add(color);
                }

                return colors;
            }

            int targetCount = entry.Type == PowerUpType.BoostThreeColors ? 3 : 2;

            ColorId activeColor = m_ScoreManager.ActiveColor;
            colors.Add(activeColor);

            List<ColorId> unlocked = m_ScoreManager.UnlockedColors;
            List<ColorId> candidates = new List<ColorId>(unlocked);

            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                ColorId temp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = temp;
            }

            for (int i = 0; i < candidates.Count && colors.Count < targetCount; i++)
            {
                if (candidates[i] != activeColor)
                {
                    colors.Add(candidates[i]);
                }
            }

            for (int i = 0; i < entry.ColorsCovered.Count && colors.Count < targetCount; i++)
            {
                if (!colors.Contains(entry.ColorsCovered[i]))
                {
                    colors.Add(entry.ColorsCovered[i]);
                }
            }

            return colors;
        }

        /// <summary>
        /// Clears the active power-ups for a new run.
        /// </summary>
        private void ResetPowerUps()
        {
            for (int i = 0; i < m_ActivePowerUps.Count; i++)
            {
                ActivePowerUp active = m_ActivePowerUps[i];
                if (active.Instance != null)
                {
                    m_Pool.Release(active.Prefab, active.Instance);
                }
            }

            m_ActivePowerUps.Clear();
            m_DistanceSinceLastPowerUp = 0f;
        }

        #endregion
    }
}
