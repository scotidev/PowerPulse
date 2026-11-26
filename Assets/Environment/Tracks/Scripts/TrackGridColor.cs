using System;
using System.Collections.Generic;
using UnityEngine;
using PowerPulse.Core;

namespace PowerPulse.Environment
{
    /// <summary>
    /// Controls the color of the neon grid lines on the track tile sides.
    /// Modifies the SHARED MATERIAL ASSET directly, so ALL grid lines (active + pooled)
    /// change color instantly. Mirrors the player's active color from ScoreManager,
    /// and flashes through all palette colors when the CollectAllColors boost is active.
    /// </summary>
    [RequireComponent(typeof(TrackTile))]
    public class TrackGridColor : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("References")]
        [SerializeField, Tooltip("Color palette asset with the five official colors and emission values.")]
        private SO_ColorPalette m_Palette;

        [SerializeField, Tooltip("The SHARED material used by ALL grid line strips. Drag the material from Project folder here (not from a renderer).")]
        private Material m_GridMaterial;

        #endregion

        #region FIELDS

        private Color m_OriginalColor = Color.white;
        private Color m_OriginalEmissionColor = Color.white;

        private ColorId m_CurrentActiveColor = ColorId.Red;
        private List<ColorId> m_BoostColors = new List<ColorId>();
        private bool m_IsBoostActive;
        private float m_FlashTimer;
        private int m_FlashIndex;
        private ScoreManager m_ScoreManager;

        // Blink intervals matching PlayerColor (starts fast ~0.08s, slows to ~0.25s)
        private const float c_BoostBlinkFastInterval = 0.08f;
        private const float c_BoostBlinkSlowInterval = 0.25f;

        #endregion

        #region UNITY CALLBACKS

        private void Awake()
        {
            CacheOriginalMaterialProperties();
        }

        private void OnEnable()
        {
            // ScoreManager is a scene object (Layer 2), not a persistent service.
            // Use FindAnyObjectByType to get it from the active scene.
            m_ScoreManager = FindAnyObjectByType<ScoreManager>();

            SubscribeToScoreManager();
            ApplyCurrentState();
        }

        private void OnDisable()
        {
            UnsubscribeFromScoreManager();
        }

        private void Update()
        {
            if (m_IsBoostActive && m_BoostColors.Count > 0)
            {
                TickFlash();
            }
        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        /// Caches the original color and emission of the shared material for restoration.
        /// </summary>
        private void CacheOriginalMaterialProperties()
        {
            if (m_GridMaterial != null)
            {
                m_OriginalColor = m_GridMaterial.color;
                m_OriginalEmissionColor = m_GridMaterial.GetColor("_EmissionColor");
            }
        }

        /// <summary>
        /// Subscribes to ScoreManager events.
        /// </summary>
        private void SubscribeToScoreManager()
        {
            if (m_ScoreManager != null)
            {
                m_ScoreManager.OnColorChanged += HandleColorChanged;
                m_ScoreManager.OnBoostChanged += HandleBoostChanged;
                m_ScoreManager.OnRunReset += HandleRunReset;
            }
            else
            {
                Debug.LogWarning($"[TrackGridColor] ScoreManager not found on {name}. Grid color sync will not work. Make sure Bootstraper has initialized services.", this);
            }
        }

        /// <summary>
        /// Unsubscribes from ScoreManager events.
        /// </summary>
        private void UnsubscribeFromScoreManager()
        {
            if (m_ScoreManager != null)
            {
                m_ScoreManager.OnColorChanged -= HandleColorChanged;
                m_ScoreManager.OnBoostChanged -= HandleBoostChanged;
                m_ScoreManager.OnRunReset -= HandleRunReset;
            }
        }

        /// <summary>
        /// Applies the current color/boost state to the shared material.
        /// </summary>
        private void ApplyCurrentState()
        {
            if (m_ScoreManager == null || m_GridMaterial == null) return;

            m_CurrentActiveColor = m_ScoreManager.ActiveColor;
            m_BoostColors = new List<ColorId>(m_ScoreManager.BoostColors);
            m_IsBoostActive = m_BoostColors.Count > 0;

            if (!m_IsBoostActive)
            {
                ApplyColor(m_CurrentActiveColor);
            }
            else
            {
                // Start flashing immediately with dynamic interval
                m_FlashTimer = GetBlinkInterval();
                m_FlashIndex = 0;
            }
        }

        /// <summary>
        /// Applies a specific color to the SHARED grid material.
        /// </summary>
        private void ApplyColor(ColorId colorId)
        {
            if (m_Palette == null || m_GridMaterial == null) return;

            Color baseColor = m_Palette.GetColor(colorId);
            float emission = m_Palette.GetEmission(colorId);

            m_GridMaterial.color = baseColor;
            m_GridMaterial.SetColor("_BaseColor", baseColor);
            m_GridMaterial.SetColor("_EmissionColor", baseColor * emission);
        }

        /// <summary>
        /// Flashes through all boost colors rapidly on the shared material.
        /// Interval matches PlayerColor: starts fast and slows down as boost expires.
        /// </summary>
        private void TickFlash()
        {
            m_FlashTimer += Time.deltaTime;

            float interval = GetBlinkInterval();

            if (m_FlashTimer < interval)
            {
                return;
            }

            m_FlashTimer = 0f;
            m_FlashIndex = (m_FlashIndex + 1) % m_BoostColors.Count;

            ColorId flashColor = m_BoostColors[m_FlashIndex];
            ApplyColor(flashColor);
        }

        /// <summary>
        /// Returns the blink interval for the current boost progress, matching PlayerColor.
        /// Starts fast (0.08s) and slows down (0.25s) as the boost runs out.
        /// </summary>
        private float GetBlinkInterval()
        {
            if (m_ScoreManager == null || m_ScoreManager.BoostDuration <= 0f)
            {
                return c_BoostBlinkSlowInterval;
            }

            float progress = Mathf.Clamp01(m_ScoreManager.BoostTimeLeft / m_ScoreManager.BoostDuration);

            // progress = 1 at start (full time left) -> fast interval
            // progress = 0 at end (no time left) -> slow interval
            return Mathf.Lerp(c_BoostBlinkFastInterval, c_BoostBlinkSlowInterval, progress);
        }

        #endregion

        #region EVENT HANDLERS

        /// <summary>
        /// Called when the active color changes (and no boost is active).
        /// </summary>
        private void HandleColorChanged(ColorId newColor)
        {
            m_CurrentActiveColor = newColor;

            if (!m_IsBoostActive && m_GridMaterial != null)
            {
                ApplyColor(newColor);
            }
        }

        /// <summary>
        /// Called when the boost state changes.
        /// </summary>
        private void HandleBoostChanged(List<ColorId> boostColors)
        {
            m_BoostColors = boostColors != null ? new List<ColorId>(boostColors) : new List<ColorId>();
            m_IsBoostActive = m_BoostColors.Count > 0;

            if (m_IsBoostActive)
            {
                // Start flashing immediately with dynamic interval
                m_FlashTimer = GetBlinkInterval();
                m_FlashIndex = 0;
            }
            else
            {
                // Boost ended - return to active color
                if (m_GridMaterial != null)
                {
                    ApplyColor(m_CurrentActiveColor);
                }
            }
        }

        /// <summary>
        /// Called when a new run starts - reset to initial state.
        /// </summary>
        private void HandleRunReset()
        {
            m_CurrentActiveColor = m_ScoreManager != null ? m_ScoreManager.ActiveColor : ColorId.Red;
            m_BoostColors.Clear();
            m_IsBoostActive = false;
            m_FlashTimer = 0f;
            m_FlashIndex = 0;

            if (m_GridMaterial != null)
            {
                ApplyColor(m_CurrentActiveColor);
            }
        }

        #endregion
    }
}