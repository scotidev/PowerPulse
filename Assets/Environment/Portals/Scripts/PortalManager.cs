using System.Collections.Generic;
using UnityEngine;
using PowerPulse.Core;

namespace PowerPulse.Environment
{
    /// <summary>
    /// Spawns the color portals at the configured distance interval and recycles
    /// the ones behind the camera. Chooses the next color from the palette unlock
    /// order, so the progression is guaranteed and never random.
    /// </summary>
    public class PortalManager : MonoBehaviour
    {
        #region STATIC

        /// <summary>Fallback interval when neither the Inspector nor the balance asset defines one.</summary>
        private const float c_DefaultPortalInterval = 500f;

        #endregion


        #region SERIALIZED FIELDS

        [Header("References")]

        [SerializeField, Tooltip("Run manager that provides the current distance.")]
        private RunManager m_RunManager;

        [SerializeField, Tooltip("Score manager that owns the active color and the unlock order.")]
        private ScoreManager m_ScoreManager;

        [SerializeField, Tooltip("Shared object pool used to recycle the portals.")]
        private ObjectPool m_Pool;

        [SerializeField, Tooltip("Track manager that provides the front edge of the track.")]
        private TrackManager m_TrackManager;

        [SerializeField, Tooltip("Spawn manager that opens the adaptation window when a portal is crossed.")]
        private SpawnManager m_SpawnManager;

        [SerializeField, Tooltip("Color palette with the unlock order.")]
        private SO_ColorPalette m_Palette;

        [SerializeField, Tooltip("Game balance asset with the portal interval. Used when the field below is 0.")]
        private Config_GameBalance m_Balance;

        [SerializeField, Tooltip("Portal prefab.")]
        private GameObject m_PortalPrefab;

        [Header("Rules")]

        [SerializeField, Tooltip("No portal spawns before this distance, in meters.")]
        private float m_InitialSafeDistance = 500f;

        [SerializeField, Tooltip("Distance between portals in meters. Uses the GameBalance value by default (0 = use config).")]
        private float m_PortalInterval = 0f;

        [SerializeField, Tooltip("World Z below which a portal is recycled.")]
        private float m_RecycleZ = -15f;

        [SerializeField, Tooltip("How many portal instances are pre-created.")]
        private int m_PortalPrewarm = 2;

        #endregion


        #region FIELDS

        private readonly List<GameObject> m_ActivePortals = new List<GameObject>();
        private float m_DistanceSinceLastPortal;
        private int m_NextColorIndex;

        #endregion


        #region UNITY CALLBACKS

        private void Start()
        {
            if (m_Pool == null || m_RunManager == null || m_TrackManager == null || m_ScoreManager == null)
            {
                Debug.LogError("[PortalManager] Missing references. Assign the pool, the run manager, the track manager and the score manager.", this);
                enabled = false;
                return;
            }

            if (m_PortalPrefab == null)
            {
                Debug.LogError("[PortalManager] The portal prefab is not assigned.", this);
                enabled = false;
                return;
            }

            if (m_PortalPrefab.GetComponent<Portal>() == null)
            {
                Debug.LogError("[PortalManager] The portal prefab needs a Portal component.", this);
                enabled = false;
                return;
            }

            m_Pool.CreatePool(m_PortalPrefab, m_PortalPrewarm);
            m_NextColorIndex = m_ScoreManager.UnlockedColors.Count;
        }

        private void Update()
        {
            if (!m_RunManager.IsRunning)
            {
                return;
            }

            float delta = Time.deltaTime * m_RunManager.Speed;
            MovePortals(delta);
            TrySpawn(delta);
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Resets the portal state for a new run: clears the active portals and the
        /// distance accumulator.
        /// </summary>
        public void ResetRun()
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
        /// Applies the color change when the player crosses a portal: the portal
        /// becomes the active color and the next color in the unlock order is
        /// unlocked. Lives here because this manager is a scene object and can
        /// reach the ScoreManager.
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

            // Adaptation window: the player gets a clear stretch of track to read
            // the new color before obstacles resume.
            GetSpawnManager()?.OpenPortalCooldown();

            Debug.Log($"[PortalManager] Player crossed. New active color: {newColor} | Unlocked: {unlocked}");
        }

        /// <summary>
        /// Returns the spawn manager, finding it in the scene when it was not
        /// assigned in the Inspector.
        /// </summary>
        /// <returns>The spawn manager, or null.</returns>
        private SpawnManager GetSpawnManager()
        {
            if (m_SpawnManager == null)
            {
                m_SpawnManager = FindFirstObjectByType<SpawnManager>();
            }

            return m_SpawnManager;
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Moves every active portal toward the camera and recycles the ones behind
        /// the recycle line.
        /// </summary>
        /// <param name="delta">Distance to move in world units.</param>
        private void MovePortals(float delta)
        {
            for (int i = m_ActivePortals.Count - 1; i >= 0; i--)
            {
                GameObject portal = m_ActivePortals[i];
                portal.transform.position += Vector3.back * delta;

                if (portal.transform.position.z < m_RecycleZ)
                {
                    m_Pool.Release(m_PortalPrefab, portal);
                    m_ActivePortals.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Spawns a new portal when the configured interval is reached.
        /// </summary>
        /// <param name="delta">Distance moved this frame, in meters.</param>
        private void TrySpawn(float delta)
        {
            m_DistanceSinceLastPortal += delta;

            if (m_DistanceSinceLastPortal < m_InitialSafeDistance)
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
        /// Spawns a portal at the front of the track with the next color in the
        /// unlock order. When every color is already unlocked, the portal grants a
        /// random color among the unlocked ones.
        /// </summary>
        private void SpawnPortal()
        {
            if (m_ActivePortals.Count >= m_PortalPrewarm)
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

            instance.transform.position = new Vector3(0f, 0f, m_TrackManager.FrontZ);
            m_ActivePortals.Add(instance);

            // A portal must never arrive together with a row of obstacles: the
            // player needs a clear stretch to read the new color.
            GetSpawnManager()?.ClearObstaclesNear(instance.transform.position.z);

            Debug.Log($"[PortalManager] Portal spawned at {m_RunManager.Distance:0} m with color {portal.TargetColor}.");
        }

        /// <summary>
        /// Returns the next color in the unlock order. Once every color is
        /// unlocked, picks randomly among the unlocked ones so the portals keep
        /// changing the active color.
        /// </summary>
        /// <returns>The color the next portal will grant.</returns>
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

        #endregion
    }
}
