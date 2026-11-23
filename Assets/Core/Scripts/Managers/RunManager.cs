using System;
using System.Collections.Generic;
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

        [SerializeField, Tooltip("VFX settings asset for boost speed/FOV return timing.")]
        private Config_VFX m_VfxConfig;

        #endregion


        #region FIELDS

        private IGameManager m_GameManager;
        private Config_GameBalance.DifficultyMilestone m_ActiveMilestone;
        private int m_AppliedMilestoneIndex = -1;

        private ScoreManager m_ScoreManager;
        private bool m_BoostActive;
        private float m_BoostTimeLeft;
        private float m_BoostDuration;
        private float m_BaseSpeed;

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

        /// <summary>
        /// Whether the run has already started. Kept separately from
        /// <see cref="IsRunning"/> so pausing does not make a paused run look
        /// like a run that never started (which would restart it on resume).
        /// </summary>
        public bool HasStarted { get; private set; }

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
            m_ScoreManager = FindFirstObjectByType<ScoreManager>();
        }

        private void OnEnable()
        {
            if (m_GameManager != null)
            {
                m_GameManager.StateChanged += HandleStateChanged;
            }

            if (m_ScoreManager != null)
            {
                m_ScoreManager.OnBoostChanged += HandleBoostChanged;
            }
        }

        private void OnDisable()
        {
            if (m_GameManager != null)
            {
                m_GameManager.StateChanged -= HandleStateChanged;
            }

            if (m_ScoreManager != null)
            {
                m_ScoreManager.OnBoostChanged -= HandleBoostChanged;
            }
        }

        private void Start()
        {
            Reset();

            // The Game scene can load while the state is already Playing: the
            // StateChanged event fired before this component subscribed, so the
            // run would never start. Applying the current state here covers both
            // that case and a cold start.
            if (m_GameManager != null)
            {
                HandleStateChanged(m_GameManager.CurrentState);
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
            HasStarted = true;
            IsRunning = true;
            OnRunStarted?.Invoke();
        }

        /// <summary>
        /// Restores a paused run without resetting the progression.
        /// Does NOT invoke OnRunStarted — that event is only for NEW runs (StartRun).
        /// </summary>
        public void RestoreRun()
        {
            if (!HasStarted)
            {
                return;
            }

            IsRunning = true;
            // OnRunStarted NÃO é invocado aqui: evita que ScoreManager.Reset() limpe a run.
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
            m_BaseSpeed = Speed;
            Distance = 0f;
            Difficulty = 0f;
            HasStarted = false;
            m_ActiveMilestone = null;
            m_AppliedMilestoneIndex = -1;
            m_BoostActive = false;
            m_BoostTimeLeft = 0f;
            m_BoostDuration = 0f;
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Advances the run by the given delta time: accumulates distance and
        /// recalculates speed and difficulty. Applies boost speed multiplier
        /// and starts returning to base speed before boost expires.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        private void Tick(float deltaTime)
        {
            // Update boost time left
            if (m_BoostActive && m_BoostTimeLeft > 0f)
            {
                m_BoostTimeLeft -= deltaTime;
            }

            // Calculate base speed from milestones/curve (without boost)
            float normalizedDistance = m_Balance != null
                ? Mathf.Clamp01(Distance / Mathf.Max(m_Balance.SpeedDistanceReference, 0.0001f))
                : 0f;

            ApplyMilestones();

            m_BaseSpeed = m_Balance != null
                ? Mathf.Lerp(m_Balance.InitialSpeed, m_Balance.MaxSpeed, m_Balance.SpeedCurve.Evaluate(normalizedDistance))
                : 0f;

            // Apply boost speed multiplier with early return
            float currentSpeed = m_BaseSpeed;

            if (m_BoostActive && m_VfxConfig != null)
            {
                float returnTime = m_VfxConfig.BoostSpeedReturnTime;
                float progress = 0f;

                if (m_BoostDuration > 0f && returnTime > 0f)
                {
                    // How far into the "return phase" we are (0 = just entered return phase, 1 = boost expired)
                    progress = 1f - Mathf.Clamp01(m_BoostTimeLeft / returnTime);
                }

                if (progress > 0f)
                {
                    // In return phase: lerp from boosted speed back to base speed using the curve
                    float curveValue = m_VfxConfig.BoostReturnCurve.Evaluate(progress);
                    float boostedSpeed = m_BaseSpeed * m_VfxConfig.BoostSpeedMultiplier;
                    currentSpeed = Mathf.Lerp(boostedSpeed, m_BaseSpeed, curveValue);
                }
                else
                {
                    // Full boost phase
                    currentSpeed = m_BaseSpeed * m_VfxConfig.BoostSpeedMultiplier;
                }
            }

            Speed = currentSpeed;
            Distance += Speed * deltaTime;
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
            // A run starts exactly once, when the Game scene loads and the state
            // is already Playing. Resume/Restart must not go through this path
            // again: they manage the run themselves.
            if (state == GameState.Playing && !HasStarted)
            {
                StartRun();
            }

            switch (state)
            {
                case GameState.Paused:
                case GameState.GameOver:
                case GameState.MainMenu:
                    IsRunning = false;
                    break;
            }
        }

        /// <summary>
        /// Handles boost start/end from ScoreManager. Tracks boost timing for
        /// early speed return before boost expires.
        /// </summary>
        /// <param name="colors">Colors covered by the boost (empty = boost ended).</param>
        private void HandleBoostChanged(List<ColorId> colors)
        {
            m_BoostActive = colors != null && colors.Count > 0;

            if (m_BoostActive && m_ScoreManager != null)
            {
                m_BoostDuration = m_ScoreManager.BoostDuration;
                m_BoostTimeLeft = m_ScoreManager.BoostTimeLeft;
            }
            else
            {
                m_BoostDuration = 0f;
                m_BoostTimeLeft = 0f;
                // Ensure speed returns to base when boost ends
                Speed = m_BaseSpeed;
            }
        }

        #endregion
    }
}
