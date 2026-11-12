using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Generic GameObject pool. One instance lives in the Game scene and is shared
    /// by every system that needs pooled objects (track tiles, obstacles,
    /// power-ups, portals and VFX). Inactive instances are kept as children of a
    /// per-prefab root so the Hierarchy stays clean.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [SerializeField, Tooltip("Prefabs pooled on Start, with the amount pre-created for each one.")]
        private List<PoolConfig> m_PoolConfigs = new List<PoolConfig>();

        #endregion


        #region NESTED TYPES

        /// <summary>Configuration of a single pool: the prefab and its prewarm count.</summary>
        [Serializable]
        public class PoolConfig
        {
            [Tooltip("Prefab to pool.")]
            public GameObject Prefab;

            [Tooltip("How many instances are created on Start.")]
            public int Prewarm = 4;
        }


        /// <summary>Runtime state of a single pool.</summary>
        private class Pool
        {
            public GameObject Prefab;
            public Transform Root;
            public Queue<GameObject> Free = new Queue<GameObject>();
            public HashSet<GameObject> InUse = new HashSet<GameObject>();
            public Action<GameObject> OnGet;
            public Action<GameObject> OnRelease;
        }

        #endregion


        #region FIELDS

        private readonly Dictionary<GameObject, Pool> m_Pools = new Dictionary<GameObject, Pool>();

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            for (int i = 0; i < m_PoolConfigs.Count; i++)
            {
                PoolConfig config = m_PoolConfigs[i];
                if (config.Prefab != null)
                {
                    CreatePool(config.Prefab, config.Prewarm);
                }
            }
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Creates a pool for the given prefab. Does nothing when the prefab is
        /// <c>null</c> or already pooled.
        /// </summary>
        /// <param name="prefab">Prefab to pool.</param>
        /// <param name="prewarm">How many instances are created immediately.</param>
        /// <param name="onGet">Optional callback invoked every time an instance is retrieved.</param>
        /// <param name="onRelease">Optional callback invoked every time an instance is released.</param>
        public void CreatePool(GameObject prefab, int prewarm = 0, Action<GameObject> onGet = null, Action<GameObject> onRelease = null)
        {
            if (prefab == null || m_Pools.ContainsKey(prefab))
            {
                return;
            }

            Pool pool = new Pool
            {
                Prefab = prefab,
                Root = new GameObject($"Pool_{prefab.name}").transform,
                OnGet = onGet,
                OnRelease = onRelease
            };

            pool.Root.SetParent(transform, false);
            m_Pools.Add(prefab, pool);

            for (int i = 0; i < prewarm; i++)
            {
                GameObject instance = Instantiate(prefab, pool.Root);
                instance.SetActive(false);
                pool.Free.Enqueue(instance);
            }
        }

        /// <summary>
        /// Returns an active instance of the given prefab at the origin.
        /// </summary>
        /// <param name="prefab">Prefab to retrieve.</param>
        /// <returns>The pooled instance, or <c>null</c> when the prefab is <c>null</c>.</returns>
        public GameObject Get(GameObject prefab)
        {
            return Get(prefab, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// Returns an active instance of the given prefab at the given position and
        /// rotation. Creates a new instance when the pool is empty.
        /// </summary>
        /// <param name="prefab">Prefab to retrieve.</param>
        /// <param name="position">World position of the instance.</param>
        /// <param name="rotation">World rotation of the instance.</param>
        /// <returns>The pooled instance, or <c>null</c> when the prefab is <c>null</c>.</returns>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return null;
            }

            if (!m_Pools.TryGetValue(prefab, out Pool pool))
            {
                CreatePool(prefab);
                m_Pools.TryGetValue(prefab, out pool);
            }

            GameObject instance;

            if (pool != null && pool.Free.Count > 0)
            {
                instance = pool.Free.Dequeue();

                // Safety: never hand out an instance that is still marked as in use.
                while (pool.InUse.Contains(instance) && pool.Free.Count > 0)
                {
                    instance = pool.Free.Dequeue();
                }
            }
            else
            {
                instance = Instantiate(prefab, pool != null ? pool.Root : transform);
            }

            pool?.InUse.Add(instance);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            pool?.OnGet?.Invoke(instance);

            return instance;
        }

        /// <summary>
        /// Deactivates an instance and returns it to the pool of its prefab.
        /// Releasing the same instance twice is a no-op, so a double recycle
        /// cannot duplicate the object in the pool.
        /// </summary>
        /// <param name="prefab">Prefab the instance belongs to.</param>
        /// <param name="instance">Instance to release.</param>
        public void Release(GameObject prefab, GameObject instance)
        {
            if (prefab == null || instance == null)
            {
                return;
            }

            if (!m_Pools.TryGetValue(prefab, out Pool pool))
            {
                return;
            }

            // Guard: an instance already in the free queue is not released again.
            if (!pool.InUse.Remove(instance))
            {
                return;
            }

            pool.OnRelease?.Invoke(instance);
            instance.SetActive(false);
            instance.transform.SetParent(pool.Root, false);
            pool.Free.Enqueue(instance);
        }

        #endregion
    }
}
