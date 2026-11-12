using System;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Owns the run progression: current speed, distance traveled and difficulty
    /// level. Reads the balance configuration and reacts to the global game state
    /// (pause and game over). Lives in the Game scene.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        // NOTE: Distance is owned by RunManager during Phase 5.
        // In Phase 6, ScoreManager becomes the owner (per GDD) and RunManager
        // should read the distance from it instead of accumulating it here.

        #region SERIALIZED FIELDS

        [Header("Config")]
        [SerializeField, Tooltip("Game balance asset with the speed and difficulty curves.")]
        private Config_GameBalance m_Balance;

        [Header("State")]
        [SerializeField, Tooltip("Starts the run automatically when the scene loads.")]
        private bool m_AutoStart = true;

        #endregion


        #region FIELDS

        private IGameManager m_GameManager;
        private Config_GameBalance.DifficultyMilestone m_ActiveMilestone;
        private int m_AppliedMilestoneIndex = -1;

        #endregion


        #region PROPERTIES

        /// <summary>Current track speed in units per second.</summary>
        public float Speed { get; private set; }

        /// <summary>Distance traveled during the current run, in meters.</summary>
        public float Distance { get; private set; }

        /// <summary>Normalized difficulty level (0 to 1) based on the distance.</summary>
        public float Difficulty { get; private set; }

        /// <summary>Whether the run is currently running.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Milestone currently applied to the run (null before the first one).</summary>
        public Config_GameBalance.DifficultyMilestone ActiveMilestone => m_ActiveMilestone;

        /// <summary>Whether lateral moving obstacles are enabled by the current milestone.</summary>
        public bool MovingEnabled => m_ActiveMilestone != null && m_ActiveMilestone.EnableMoving;

        /// <summary>Whether faster lateral obstacles are enabled by the current milestone.</summary>
        public bool FasterEnabled => m_ActiveMilestone != null && m_ActiveMilestone.EnableFaster;

        /// <summary>Maximum number of obstacles per row from the current milestone.</summary>
        public int MaxObstaclesPerRow => m_ActiveMilestone != null ? m_ActiveMilestone.MaxObstaclesPerRow : 1;

        #endregion


        #region EVENTS

        /// <summary>Raised when the run starts or restarts.</summary>
        public event Action OnRunStarted;

        /// <summary>Raised when the run ends (game over).</summary>
        public event Action OnRunEnded;

        /// <summary>Raised every frame with the current distance, while running.</summary>
        public event Action<float> OnDistanceChanged;

        /// <summary>Raised every frame with the current difficulty, while running.</summary>
        public event Action<float> OnDifficultyChanged;

        /// <summary>Raised when a new milestone is applied, with the milestone.</summary>
        public event Action<Config_GameBalance.DifficultyMilestone> OnMilestoneReached;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_GameManager = ServiceLocator.Current != null ? ServiceLocator.Current.Get<IGameManager>() : null;
        }

        private void OnEnable()
        {
            if (m_GameManager != null)
            {
                m_GameManager.StateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (m_GameManager != null)
            {
                m_GameManager.StateChanged -= HandleStateChanged;
            }
        }

        private void Start()
        {
            Reset();

            if (m_AutoStart)
            {
                StartRun();
            }
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Resets the progression and starts a new run.
        /// </summary>
        public void StartRun()
        {
            Reset();
            IsRunning = true;
            OnRunStarted?.Invoke();
        }

        /// <summary>
        /// Ends the current run. Does nothing when the run is not running.
        /// </summary>
        public void EndRun()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            OnRunEnded?.Invoke();
        }

        /// <summary>
        /// Resets speed, distance and difficulty to the initial values.
        /// </summary>
        public void Reset()
        {
            Speed = m_Balance != null ? m_Balance.InitialSpeed : 0f;
            Distance = 0f;
            Difficulty = 0f;
            m_ActiveMilestone = null;
            m_AppliedMilestoneIndex = -1;
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Advances the run by the given delta time: accumulates distance and
        /// recalculates speed and difficulty.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        private void Tick(float deltaTime)
        {
            Distance += Speed * deltaTime;

            float normalizedDistance = m_Balance != null
                ? Mathf.Clamp01(Distance / Mathf.Max(m_Balance.SpeedDistanceReference, 0.0001f))
                : 0f;

            ApplyMilestones();

            Speed = m_Balance != null
                ? Mathf.Lerp(m_Balance.InitialSpeed, m_Balance.MaxSpeed, m_Balance.SpeedCurve.Evaluate(normalizedDistance))
                : 0f;

            Difficulty = normalizedDistance;

            OnDistanceChanged?.Invoke(Distance);
            OnDifficultyChanged?.Invoke(Difficulty);
        }

        /// <summary>
        /// Applies the next milestone when the distance reaches it. Runs once per
        /// milestone, never per frame, so the difficulty is decided on events
        /// instead of being recalculated continuously.
        /// </summary>
        private void ApplyMilestones()
        {
            if (m_Balance == null || m_Balance.Milestones.Count == 0)
            {
                return;
            }

            int nextIndex = m_AppliedMilestoneIndex + 1;

            if (nextIndex >= m_Balance.Milestones.Count)
            {
                return;
            }

            Config_GameBalance.DifficultyMilestone milestone = m_Balance.Milestones[nextIndex];

            if (Distance < milestone.Distance)
            {
                return;
            }

            m_AppliedMilestoneIndex = nextIndex;
            m_ActiveMilestone = milestone;
            Speed = milestone.Speed;

            OnMilestoneReached?.Invoke(milestone);

            Debug.Log($"[RunManager] Milestone reached at {milestone.Distance} m: speed {milestone.Speed}, " +
                      $"moving {milestone.EnableMoving}, faster {milestone.EnableFaster}, " +
                      $"max per row {milestone.MaxObstaclesPerRow}.");
        }

        /// <summary>
        /// Reacts to the global game state: runs during gameplay and pauses otherwise.
        /// </summary>
        /// <param name="state">New global game state.</param>
        private void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                case GameState.Tutorial:
                    IsRunning = true;
                    break;

                case GameState.Paused:
                case GameState.GameOver:
                case GameState.MainMenu:
                    IsRunning = false;
                    break;
            }
        }

        #endregion
    }
}
