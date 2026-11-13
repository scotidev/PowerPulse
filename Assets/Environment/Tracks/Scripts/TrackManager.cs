using System.Collections.Generic;
using UnityEngine;
using PowerPulse.Core;

namespace PowerPulse.Environment
{
    /// <summary>
    /// Moves the track tiles toward the camera and recycles the ones behind it,
    /// keeping a constant number of tiles active. Exposes the front edge of the
    /// track so the SpawnManager can place obstacles at the visible boundary.
    /// </summary>
    public class TrackManager : MonoBehaviour
    {
        // NOTE: Tile management is separate during Phase 5.
        // In a future refactor, move all tile logic into SpawnManager (per GDD).

        #region SERIALIZED FIELDS

        [Header("References")]
        [SerializeField, Tooltip("Run manager that provides the current track speed.")]
        private RunManager m_RunManager;

        [SerializeField, Tooltip("Shared object pool used to recycle tiles.")]
        private ObjectPool m_Pool;

        [SerializeField, Tooltip("Track tile prefab.")]
        private GameObject m_TilePrefab;

        [Header("Setup")]
        [SerializeField, Tooltip("Number of tiles kept active at the same time. Must be enough to cover the visible track plus the area behind the camera.")]
        private int m_TileCount = 9;

        [SerializeField, Tooltip("World Z below which a tile is recycled.")]
        private float m_RecycleZ = -15f;

        [SerializeField, Tooltip("World Z of the first tile when the scene starts. Negative values keep the track behind the camera.")]
        private float m_InitialTileZ = -20f;

        [SerializeField, Tooltip("Spawn manager moved together with the tiles so obstacles and tiles never drift apart.")]
        private SpawnManager m_SpawnManager;

        #endregion


        #region FIELDS

        private readonly List<GameObject> m_ActiveTiles = new List<GameObject>();
        private float m_TileLength;
        private float m_FrontZ;

        #endregion


        #region PROPERTIES

        /// <summary>World Z of the front edge of the track.</summary>
        public float FrontZ => m_FrontZ;

        #endregion


        #region UNITY CALLBACKS

        private void Start()
        {
            if (m_Pool == null || m_TilePrefab == null || m_RunManager == null)
            {
                Debug.LogError("[TrackManager] Missing references. Assign the pool, the tile prefab and the run manager.", this);
                enabled = false;
                return;
            }

            if (m_TileCount <= 0)
            {
                Debug.LogError("[TrackManager] Tile count must be greater than zero.", this);
                enabled = false;
                return;
            }

            TrackTile tileComponent = m_TilePrefab.GetComponent<TrackTile>();

            if (tileComponent == null)
            {
                Debug.LogError("[TrackManager] The tile prefab needs a TrackTile component.", this);
                enabled = false;
                return;
            }

            m_TileLength = tileComponent.Length;
            CreateInitialTiles();
        }

        private void Update()
        {
            if (!m_RunManager.IsRunning)
            {
                return;
            }

            float delta = Time.deltaTime * m_RunManager.Speed;

            // The TrackManager owns the movement of the whole scene: tiles AND
            // every spawned element (obstacles, portals, power-ups) move by the
            // same distance in the same frame, so they can never drift apart.
            MoveTiles(delta);
            m_SpawnManager?.MoveSceneWithTrack(delta);

            UpdateFrontZ();
            RecycleTiles();
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Creates the initial row of tiles, spaced by the tile length.
        /// </summary>
        private void CreateInitialTiles()
        {
            for (int i = 0; i < m_TileCount; i++)
            {
                float z = m_InitialTileZ + i * m_TileLength;
                GameObject tile = m_Pool.Get(m_TilePrefab, new Vector3(0f, 0f, z), Quaternion.identity);
                m_ActiveTiles.Add(tile);
            }

            UpdateFrontZ();
        }

        /// <summary>
        /// Moves every active tile toward the camera by the given distance.
        /// </summary>
        /// <param name="delta">Distance to move in world units.</param>
        private void MoveTiles(float delta)
        {
            for (int i = 0; i < m_ActiveTiles.Count; i++)
            {
                m_ActiveTiles[i].transform.position += Vector3.back * delta;
            }
        }

        /// <summary>
        /// Recycles the tiles behind the recycle line and refills the front of the
        /// track, keeping the active tile count constant.
        /// </summary>
        private void RecycleTiles()
        {
            for (int i = m_ActiveTiles.Count - 1; i >= 0; i--)
            {
                GameObject tile = m_ActiveTiles[i];

                if (tile.transform.position.z + m_TileLength * 0.5f < m_RecycleZ)
                {
                    m_Pool.Release(m_TilePrefab, tile);
                    m_ActiveTiles.RemoveAt(i);
                }
            }

            // Refill the front, one tile at a time. The new tile is placed right
            // after the current front tile, so the front edge advances by exactly
            // one tile length and never overlaps or gaps.
            while (m_ActiveTiles.Count < m_TileCount)
            {
                float centerZ = GetFrontTileCenterZ() + m_TileLength;
                GameObject tile = m_Pool.Get(m_TilePrefab, new Vector3(0f, 0f, centerZ), Quaternion.identity);
                m_ActiveTiles.Add(tile);
                UpdateFrontZ();
            }
        }

        /// <summary>
        /// Recalculates the front edge of the track from the active tiles.
        /// </summary>
        private void UpdateFrontZ()
        {
            m_FrontZ = GetFrontTileCenterZ() + m_TileLength * 0.5f;
        }

        /// <summary>
        /// Returns the center Z of the tile furthest from the camera, or the
        /// initial tile Z when the track is empty.
        /// </summary>
        /// <returns>Center Z of the front tile.</returns>
        private float GetFrontTileCenterZ()
        {
            if (m_ActiveTiles.Count == 0)
            {
                return m_InitialTileZ;
            }

            float maxZ = float.NegativeInfinity;

            for (int i = 0; i < m_ActiveTiles.Count; i++)
            {
                maxZ = Mathf.Max(maxZ, m_ActiveTiles[i].transform.position.z);
            }

            return maxZ;
        }

        #endregion
    }
}
