using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Configuration asset for the Bootstraper. Holds references to prefabs
    /// and other bootstrap-time assets so the static Bootstraper doesn't need
    /// hardcoded Resources paths scattered throughout the code.
    /// Place this asset in any Resources folder (e.g., Assets/Core/Resources/BootstrapConfig.asset).
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/Bootstrap Config", fileName = "BootstrapConfig")]
    public class BootstrapConfig : ScriptableObject
    {
        #region SERIALIZED FIELDS

        [Header("Persistent Services Prefabs")]
        [SerializeField, Tooltip("UI Manager prefab with all panels and references configured in Inspector.")]
        private GameObject m_UIManagerPrefab;

        #endregion

        #region PROPERTIES

        /// <summary>UI Manager prefab with all panels and references configured in Inspector.</summary>
        public GameObject UIManagerPrefab => m_UIManagerPrefab;

        #endregion
    }
}