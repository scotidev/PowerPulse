using System;
using System.Collections.Generic;
using UnityEngine;
using PowerPulse.Core;
using PowerPulse.Obstacles;

namespace PowerPulse.Player
{
    /// <summary>
    /// Visual mirror of the ScoreManager: holds the active color, the shield
    /// flag and the boost colors, and applies them to the player material.
    /// It also exposes the collect/die decision to the PlayerCollision, so the
    /// collision component never needs to know that the ScoreManager exists.
    /// It never decides rules — it only reflects the state published by the
    /// ScoreManager through the ColorChanged/ShieldChanged/BoostChanged events.
    /// </summary>
    public class PlayerColor : MonoBehaviour
    {
        #region STATIC

        /// <summary>Seconds between color swaps while a boost is active.</summary>
        private const float c_BoostBlinkInterval = 0.25f;

        #endregion


        #region SERIALIZED FIELDS

        [Header("References")]

        [SerializeField, Tooltip("Score manager that owns the active color, the shield and the boost.")]
        private ScoreManager m_ScoreManager;

        [SerializeField, Tooltip("Color palette asset.")]
        private SO_ColorPalette m_Palette;

        [SerializeField, Tooltip("Renderer that receives the active color.")]
        private Renderer m_Renderer;

        [Header("Shield Visual")]

        [SerializeField, Tooltip("Visual ring shown around the sphere while the shield is active.")]
        private GameObject m_ShieldVisual;

        [Header("State")]

        [SerializeField, Tooltip("Color applied on Start when the ScoreManager is not assigned.")]
        private ColorId m_InitialColor = ColorId.Red;

        #endregion


        #region FIELDS

        private Material m_MaterialInstance;
        private PlayerDeath m_Death;
        private readonly List<ColorId> m_BoostColors = new List<ColorId>();
        private float m_BoostBlinkTimer;
        private int m_BoostBlinkIndex;

        #endregion


        #region PROPERTIES

        /// <summary>Currently active color.</summary>
        public ColorId ActiveColor { get; private set; }

        /// <summary>Whether a shield is currently active.</summary>
        public bool HasShield { get; private set; }

        /// <summary>Colors covered by the active boost (empty when no boost is active).</summary>
        public List<ColorId> BoostColors => m_BoostColors;

        #endregion


        #region EVENTS

        /// <summary>Raised when the active color changes.</summary>
        public event Action<ColorId> OnColorChanged;

        /// <summary>Raised when the shield state changes.</summary>
        public event Action<bool> OnShieldChanged;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            if (m_Renderer != null)
            {
                m_MaterialInstance = m_Renderer.material;
            }

            if (m_ShieldVisual != null)
            {
                m_ShieldVisual.SetActive(false);
            }

            m_Death = GetComponent<PlayerDeath>();
        }

        private void OnEnable()
        {
            SubscribeToScoreManager();
        }

        private void OnDisable()
        {
            UnsubscribeFromScoreManager();
        }

        private void Start()
        {
            if (m_ScoreManager == null)
            {
                SetColor(m_InitialColor);
                return;
            }

            SetColor(m_ScoreManager.ActiveColor);
            ApplyShield(m_ScoreManager.HasShield);
        }

        private void Update()
        {
            TickBoostBlink();
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>Changes the active color and applies it to the material.</summary>
        /// <param name="id">New color identifier.</param>
        public void SetColor(ColorId id)
        {
            ActiveColor = id;
            ApplyColor(id);
            OnColorChanged?.Invoke(id);
        }

        /// <summary>
        /// Shows or hides the shield visual.
        /// </summary>
        /// <param name="active">True to show the shield ring.</param>
        public void ApplyShield(bool active)
        {
            HasShield = active;

            if (m_ShieldVisual != null)
            {
                m_ShieldVisual.SetActive(active);
            }

            OnShieldChanged?.Invoke(active);
        }

        /// <summary>
        /// Replaces the colors covered by the boost. An empty list clears it.
        /// </summary>
        /// <param name="colors">Colors covered by the boost.</param>
        public void ApplyBoost(List<ColorId> colors)
        {
            m_BoostColors.Clear();

            if (colors != null && colors.Count > 0)
            {
                m_BoostColors.AddRange(colors);
            }

            m_BoostBlinkTimer = 0f;
            m_BoostBlinkIndex = 0;

            if (m_BoostColors.Count == 0)
            {
                SetColor(m_ScoreManager != null ? m_ScoreManager.ActiveColor : ActiveColor);
            }
        }

        /// <summary>
        /// Tells whether an obstacle of the given color can be collected right now.
        /// Asks the ScoreManager, which is the authority of the rule.
        /// </summary>
        /// <param name="color">Color of the obstacle being touched.</param>
        /// <returns>True when the color can be collected.</returns>
        public bool CanCollect(ColorId color)
        {
            if (m_ScoreManager == null)
            {
                Debug.LogError("[PlayerColor] The ScoreManager is not assigned. The collect/die rules cannot run.", this);
                return false;
            }

            return m_ScoreManager.CanCollect(color);
        }

        /// <summary>
        /// Collects an obstacle through the ScoreManager, which owns the points.
        /// </summary>
        /// <param name="obstacle">Obstacle being collected.</param>
        public void Collect(Obstacle obstacle)
        {
            if (m_ScoreManager == null)
            {
                return;
            }

            m_ScoreManager.Collect(obstacle);
        }

        /// <summary>
        /// Forwards an incompatible collision to the ScoreManager, which decides
        /// between absorbing it with a shield and ending the run.
        /// </summary>
        /// <param name="obstacle">Obstacle with the incompatible color.</param>
        public void HandleIncompatibleHit(Obstacle obstacle)
        {
            if (m_ScoreManager == null)
            {
                return;
            }

            m_ScoreManager.HandleIncompatibleHit(obstacle);
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Subscribes to the ScoreManager events so this component always mirrors
        /// the authoritative state.
        /// </summary>
        private void SubscribeToScoreManager()
        {
            if (m_ScoreManager == null)
            {
                return;
            }

            m_ScoreManager.OnColorChanged += HandleColorChanged;
            m_ScoreManager.OnShieldChanged += HandleShieldChanged;
            m_ScoreManager.OnBoostChanged += HandleBoostChanged;
            m_ScoreManager.OnPlayerDied += HandlePlayerDied;
            m_ScoreManager.OnRunReset += HandleRunReset;
        }

        /// <summary>
        /// Unsubscribes from the ScoreManager events (mirror of OnEnable).
        /// </summary>
        private void UnsubscribeFromScoreManager()
        {
            if (m_ScoreManager == null)
            {
                return;
            }

            m_ScoreManager.OnColorChanged -= HandleColorChanged;
            m_ScoreManager.OnShieldChanged -= HandleShieldChanged;
            m_ScoreManager.OnBoostChanged -= HandleBoostChanged;
            m_ScoreManager.OnPlayerDied -= HandlePlayerDied;
            m_ScoreManager.OnRunReset -= HandleRunReset;
        }

        /// <summary>Applies the given color to the player material.</summary>
        /// <param name="id">Color identifier to apply.</param>
        private void ApplyColor(ColorId id)
        {
            if (m_MaterialInstance == null || m_Palette == null)
            {
                return;
            }

            Color color = m_Palette.GetColor(id);
            float emission = m_Palette.GetEmission(id);

            m_MaterialInstance.color = color;
            m_MaterialInstance.SetColor("_BaseColor", color);
            m_MaterialInstance.SetColor("_EmissionColor", color * emission);
        }

        /// <summary>
        /// Blinks between the boost colors while the boost is active, giving the
        /// player the visual feedback described in the GDD.
        /// </summary>
        private void TickBoostBlink()
        {
            if (m_BoostColors.Count == 0)
            {
                return;
            }

            m_BoostBlinkTimer += Time.deltaTime;

            if (m_BoostBlinkTimer < c_BoostBlinkInterval)
            {
                return;
            }

            m_BoostBlinkTimer = 0f;
            m_BoostBlinkIndex = (m_BoostBlinkIndex + 1) % m_BoostColors.Count;

            ColorId color = m_BoostColors[m_BoostBlinkIndex];
            ApplyColor(color);
            OnColorChanged?.Invoke(color);
        }

        /// <summary>Mirrors the active color published by the ScoreManager.</summary>
        /// <param name="color">New active color.</param>
        private void HandleColorChanged(ColorId color)
        {
            if (m_BoostColors.Count > 0)
            {
                return;
            }

            SetColor(color);
        }

        /// <summary>Mirrors the shield state published by the ScoreManager.</summary>
        /// <param name="active">Whether the shield is active.</param>
        private void HandleShieldChanged(bool active)
        {
            ApplyShield(active);
        }

        /// <summary>Mirrors the boost colors published by the ScoreManager.</summary>
        /// <param name="colors">Colors covered by the boost.</param>
        private void HandleBoostChanged(List<ColorId> colors)
        {
            ApplyBoost(colors);
        }

        /// <summary>
        /// Starts the death animation when the ScoreManager decides the player
        /// died. The run is only closed after the animation finishes.
        /// </summary>
        private void HandlePlayerDied()
        {
            if (m_Death == null)
            {
                Debug.LogError("[PlayerColor] The player needs a PlayerDeath component to play the death animation.", this);
                return;
            }

            m_Death.Die();
        }

        /// <summary>
        /// Restores the player state when a new run starts.
        /// </summary>
        private void HandleRunReset()
        {
            ApplyBoost(new List<ColorId>());
            ApplyShield(false);

            if (m_Death != null)
            {
                m_Death.ResetPlayer();
            }
        }

        #endregion
    }
}
