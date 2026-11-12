using System;
using System.Collections.Generic;
using UnityEngine;
using PowerPulse.Obstacles;

namespace PowerPulse.Core
{
    /// <summary>
    /// Authority of the run: the score, the local record, the active color, the
    /// unlocked colors, the shield flag and the active boost. It also owns the
    /// collect/die decision asked by the PlayerCollision. Lives in the Game
    /// scene (layer 2) and is fetched by the other scene objects through a
    /// direct [SerializeField] reference.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        #region STATIC

        /// <summary>Number of colors active at the start of a run.</summary>
        private const int c_InitialColorCount = 2;

        /// <summary>Total number of colors available in the game.</summary>
        private const int c_MaxColors = 5;

        /// <summary>Key used by the save service to persist the best score.</summary>
        private const string c_BestScoreKey = "PowerPulse.BestScore";

        #endregion


        #region SERIALIZED FIELDS

        [Header("References")]

        [SerializeField, Tooltip("Color palette asset with the five official colors and the unlock order.")]
        private SO_ColorPalette m_Palette;

        [SerializeField, Tooltip("Run manager that provides the current distance of the run.")]
        private RunManager m_RunManager;

        [SerializeField, Tooltip("Shared object pool used to recycle the collected obstacles.")]
        private ObjectPool m_Pool;

        [Header("Starting Color")]

        [SerializeField, Tooltip("Color the player starts the run with.")]
        private ColorId m_StartingColor = ColorId.Red;

        #endregion


        #region FIELDS

        private readonly List<ColorId> m_UnlockedColors = new List<ColorId>();
        private readonly List<ColorId> m_BoostColors = new List<ColorId>();
        private float m_BoostTimer;
        private bool m_HasShield;
        private bool m_IsRunActive;
        private int m_Score;
        private int m_BestScore;

        #endregion


        #region PROPERTIES

        /// <summary>Currently active color of the player.</summary>
        public ColorId ActiveColor { get; private set; }

        /// <summary>Colors the player is allowed to collect right now.</summary>
        public List<ColorId> UnlockedColors => m_UnlockedColors;

        /// <summary>Colors covered by the active boost (empty when no boost is active).</summary>
        public List<ColorId> BoostColors => m_BoostColors;

        /// <summary>Whether a shield is currently active.</summary>
        public bool HasShield => m_HasShield;

        /// <summary>Whether a boost is currently active.</summary>
        public bool HasBoost => m_BoostColors.Count > 0;

        /// <summary>Points accumulated in the current run.</summary>
        public int Score => m_Score;

        /// <summary>Best score persisted locally.</summary>
        public int BestScore => m_BestScore;

        /// <summary>Distance traveled in the current run, read from the RunManager.</summary>
        public float Distance => m_RunManager != null ? m_RunManager.Distance : 0f;

        /// <summary>Remaining boost time in seconds.</summary>
        public float BoostTimeLeft => m_BoostTimer;

        #endregion


        #region EVENTS

        /// <summary>Raised when the score changes, with the new score.</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>Raised when the shield state changes.</summary>
        public event Action<bool> OnShieldChanged;

        /// <summary>Raised when a boost starts or ends, with the covered colors.</summary>
        public event Action<List<ColorId>> OnBoostChanged;

        /// <summary>Raised when the active color changes.</summary>
        public event Action<ColorId> OnColorChanged;

        /// <summary>Raised when an obstacle is collected: points and world position.</summary>
        public event Action<int, Vector3> OnCollected;

        /// <summary>Raised when the player dies (no shield left).</summary>
        public event Action OnPlayerDied;

        /// <summary>Raised when the record is beaten, with the new record.</summary>
        public event Action<int> OnRecordBeaten;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            ActiveColor = m_StartingColor;
        }

        private void OnEnable()
        {
            LoadBestScore();
            Reset();
        }

        private void Update()
        {
            TickBoost();
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Resets the score, the colors and the shield for a new run.
        /// </summary>
        public void Reset()
        {
            m_Score = 0;
            m_BoostTimer = 0f;
            m_HasShield = false;
            m_IsRunActive = true;
            m_BoostColors.Clear();

            BuildUnlockedColors();
            ActiveColor = m_StartingColor;

            OnScoreChanged?.Invoke(m_Score);
            OnShieldChanged?.Invoke(m_HasShield);
            OnBoostChanged?.Invoke(m_BoostColors);
            OnColorChanged?.Invoke(ActiveColor);
        }

        /// <summary>
        /// Tells whether the player can collect an obstacle of the given color.
        /// True only when the color is the active one or one of the colors
        /// covered by the active boost. Colors that are merely unlocked but are
        /// not active are still lethal, as described in the GDD.
        /// </summary>
        /// <param name="color">Color of the obstacle being touched.</param>
        /// <returns>True when the color can be collected.</returns>
        public bool CanCollect(ColorId color)
        {
            if (!m_IsRunActive)
            {
                return false;
            }

            if (color == ActiveColor)
            {
                return true;
            }

            return m_BoostColors.Contains(color);
        }

        /// <summary>
        /// Collects an obstacle: adds its score, publishes the events and
        /// deactivates the obstacle through the pool.
        /// </summary>
        /// <param name="obstacle">Obstacle being collected.</param>
        public void Collect(Obstacle obstacle)
        {
            if (obstacle == null)
            {
                return;
            }

            int points = obstacle.ScoreValue;
            Vector3 position = obstacle.transform.position;

            m_Score += points;

            OnScoreChanged?.Invoke(m_Score);
            OnCollected?.Invoke(points, position);
            ScoreFeedback(position, points);

            // The obstacle is recycled AFTER the events, so the listeners still
            // see a live transform (VFX position, SFX, HUD update).
            RecycleObstacle(obstacle);
        }

        /// <summary>
        /// Returns a collected obstacle to the pool, hiding it immediately so
        /// the collection reads as an instant pickup.
        /// </summary>
        /// <param name="obstacle">Obstacle being collected.</param>
        private void RecycleObstacle(Obstacle obstacle)
        {
            GameObject instance = obstacle.gameObject;
            GameObject prefab = obstacle.SourcePrefab != null ? obstacle.SourcePrefab : instance;

            if (m_Pool != null)
            {
                m_Pool.Release(prefab, instance);
            }
            else
            {
                instance.SetActive(false);
            }
        }

        /// <summary>
        /// Handles a collision with an incompatible color. The shield absorbs it
        /// once when active; otherwise the player dies.
        /// </summary>
        /// <param name="obstacle">Obstacle with the incompatible color.</param>
        public void HandleIncompatibleHit(Obstacle obstacle)
        {
            if (!m_IsRunActive)
            {
                return;
            }

            if (m_HasShield)
            {
                ConsumeShield();
                OnCollected?.Invoke(0, obstacle != null ? obstacle.transform.position : transform.position);
                return;
            }

            Die();
        }

        /// <summary>
        /// Moves the player to the given color, ending the run when the color is
        /// not one of the unlocked ones.
        /// </summary>
        /// <param name="color">Color the player is trying to assume.</param>
        /// <returns>True when the color was applied, false when the player died.</returns>
        public bool TrySetActiveColor(ColorId color)
        {
            if (!m_IsRunActive)
            {
                return false;
            }

            if (!CanCollect(color))
            {
                HandleIncompatibleHit(null);
                return false;
            }

            SetActiveColor(color);
            return true;
        }

        /// <summary>
        /// Grants a shield that absorbs one incompatible collision.
        /// </summary>
        public void AddShield()
        {
            m_HasShield = true;
            OnShieldChanged?.Invoke(m_HasShield);
        }

        /// <summary>
        /// Activates a boost that allows collecting the given colors for the
        /// given duration.
        /// </summary>
        /// <param name="colors">Colors covered by the boost.</param>
        /// <param name="duration">Duration in seconds.</param>
        public void ActivateBoost(List<ColorId> colors, float duration)
        {
            m_BoostColors.Clear();

            if (colors != null && colors.Count > 0)
            {
                m_BoostColors.AddRange(colors);
            }

            m_BoostTimer = Mathf.Max(0f, duration);
            OnBoostChanged?.Invoke(m_BoostColors);
        }

        /// <summary>
        /// Changes the active color of the player.
        /// </summary>
        /// <param name="color">New active color.</param>
        public void SetActiveColor(ColorId color)
        {
            if (ActiveColor == color)
            {
                return;
            }

            ActiveColor = color;
            OnColorChanged?.Invoke(ActiveColor);
        }

        /// <summary>
        /// Unlocks the next color in the palette order, up to the maximum.
        /// </summary>
        /// <returns>The color that was unlocked, or the active color when the maximum is reached.</returns>
        public ColorId UnlockNextColor()
        {
            List<ColorId> order = m_Palette != null ? m_Palette.GetUnlockOrder() : null;

            if (order == null)
            {
                return ActiveColor;
            }

            for (int i = 0; i < order.Count; i++)
            {
                if (!m_UnlockedColors.Contains(order[i]))
                {
                    m_UnlockedColors.Add(order[i]);
                    return order[i];
                }
            }

            return ActiveColor;
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Builds the initial list of unlocked colors from the palette order.
        /// </summary>
        private void BuildUnlockedColors()
        {
            m_UnlockedColors.Clear();

            List<ColorId> order = m_Palette != null ? m_Palette.GetUnlockOrder() : null;

            if (order == null)
            {
                m_UnlockedColors.Add(m_StartingColor);
                return;
            }

            int count = Mathf.Clamp(c_InitialColorCount, 1, Mathf.Min(c_MaxColors, order.Count));

            for (int i = 0; i < count; i++)
            {
                m_UnlockedColors.Add(order[i]);
            }
        }

        /// <summary>
        /// Advances the boost timer and clears the boost when it expires.
        /// </summary>
        private void TickBoost()
        {
            if (m_BoostColors.Count == 0)
            {
                return;
            }

            m_BoostTimer -= Time.deltaTime;

            if (m_BoostTimer <= 0f)
            {
                m_BoostTimer = 0f;
                m_BoostColors.Clear();
                OnBoostChanged?.Invoke(m_BoostColors);
            }
        }

        /// <summary>
        /// Removes the shield after it absorbs one collision.
        /// </summary>
        private void ConsumeShield()
        {
            m_HasShield = false;
            OnShieldChanged?.Invoke(m_HasShield);
        }

        /// <summary>
        /// Ends the run: updates the record and publishes the death events.
        /// The global game state is only changed by <see cref="CloseRun"/>,
        /// after the death animation finishes (Phase 10 will replace the
        /// animation with the full VFX and the Game Over screen).
        /// </summary>
        private void Die()
        {
            if (!m_IsRunActive)
            {
                return;
            }

            m_IsRunActive = false;
            m_BoostColors.Clear();
            m_BoostTimer = 0f;

            if (m_Score > m_BestScore)
            {
                m_BestScore = m_Score;
                OnRecordBeaten?.Invoke(m_BestScore);
            }

            SaveBestScore();
            OnPlayerDied?.Invoke();

            Debug.Log($"[ScoreManager] Run ended. Score: {m_Score} | Best: {m_BestScore}");
        }

        /// <summary>
        /// Closes the run after the death animation: moves the global game state
        /// to GameOver, which pauses the track and lets the UI react.
        /// </summary>
        public void CloseRun()
        {
            IGameManager gameManager = ServiceLocator.Current != null ? ServiceLocator.Current.Get<IGameManager>() : null;
            gameManager?.SetState(GameState.GameOver);

            Debug.Log("[ScoreManager] Game state changed to GameOver.");
        }

        /// <summary>
        /// Reads the persisted record. Uses the save service when it is
        /// registered and falls back to PlayerPrefs until Phase 14.
        /// </summary>
        private void LoadBestScore()
        {
            ISaveService save = ServiceLocator.Current != null ? ServiceLocator.Current.Get<ISaveService>() : null;

            if (save != null)
            {
                ScoreData data = save.GetScoreData();

                if (data != null)
                {
                    m_BestScore = data.BestScore;
                    return;
                }
            }

            m_BestScore = PlayerPrefs.GetInt(c_BestScoreKey, 0);
        }

        /// <summary>
        /// Writes the record. Uses the save service when it is registered and
        /// falls back to PlayerPrefs until Phase 14.
        /// </summary>
        private void SaveBestScore()
        {
            ISaveService save = ServiceLocator.Current != null ? ServiceLocator.Current.Get<ISaveService>() : null;

            if (save != null)
            {
                save.SetBestScore(m_BestScore);
                save.Save();
                return;
            }

            PlayerPrefs.SetInt(c_BestScoreKey, m_BestScore);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Temporary feedback until the VFX system exists (Phase 10): logs the
        /// collection so the phase can be validated by the Console.
        /// </summary>
        /// <param name="position">World position of the collection.</param>
        /// <param name="points">Points awarded.</param>
        private void ScoreFeedback(Vector3 position, int points)
        {
            Debug.Log($"[ScoreManager] Collected +{points} at {position} | Score: {m_Score}");
        }

        #endregion
    }
}
