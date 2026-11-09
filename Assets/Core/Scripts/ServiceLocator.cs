using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Central registry of game services. Any system can fetch a service with
    /// <c>ServiceLocator.Current.Get&lt;T&gt;()</c>. Uses <see cref="Type"/> keys
    /// instead of strings (no allocation, no name collisions).
    /// </summary>
    public class ServiceLocator
    {
        #region STATIC

        /// <summary>Global access point to the active locator.</summary>
        public static ServiceLocator Current { get; private set; }

        /// <summary>
        /// Creates a clean locator, discarding any previous registrations.
        /// Called once by the Bootstraper before any scene loads.
        /// </summary>
        public static void Initialize()
        {
            Current = new ServiceLocator();
        }

        #endregion


        #region FIELDS

        private readonly Dictionary<Type, IGameService> m_Services = new Dictionary<Type, IGameService>();

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Registers a service under its interface type.
        /// Always register with the interface, e.g. <c>Register&lt;IGameManager&gt;(manager)</c>.
        /// </summary>
        /// <typeparam name="T">Service interface type.</typeparam>
        /// <param name="service">Service instance.</param>
        public void Register<T>(T service) where T : IGameService
        {
            Type key = typeof(T);

            if (m_Services.ContainsKey(key))
            {
                Debug.LogError($"[ServiceLocator] Service '{key.Name}' is already registered.");
                return;
            }

            m_Services.Add(key, service);
        }

        /// <summary>
        /// Returns the service registered under the given interface type.
        /// </summary>
        /// <typeparam name="T">Service interface type.</typeparam>
        /// <returns>The service instance, or <c>null</c> if it was not registered.</returns>
        public T Get<T>() where T : IGameService
        {
            Type key = typeof(T);

            if (!m_Services.TryGetValue(key, out IGameService service))
            {
                Debug.LogError($"[ServiceLocator] Service '{key.Name}' is not registered.");
                return default;
            }

            return (T)service;
        }

        /// <summary>
        /// Removes the service registered under the given interface type.
        /// </summary>
        /// <typeparam name="T">Service interface type.</typeparam>
        public void Unregister<T>() where T : IGameService
        {
            Type key = typeof(T);

            if (!m_Services.Remove(key))
            {
                Debug.LogError($"[ServiceLocator] Service '{key.Name}' is not registered.");
            }
        }

        #endregion
    }
}
