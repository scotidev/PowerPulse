using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using PowerPulse.Core;
using Object = UnityEngine.Object;

namespace PowerPulse.Core
{
    /// <summary>
    /// Single owner of every UI panel and of the in-run HUD. Lives on a
    /// persistent prefab instantiated by the Bootstraper, so it survives scene
    /// changes. Reacts to the global game state and to the run events, and is
    /// the only place that knows which panel is visible.
    /// </summary>
    public class UIManager : MonoBehaviour, IUIManager
    {
        #region SERIALIZED FIELDS

        [Header("Config")]
        [SerializeField, Tooltip("Input actions asset that owns the UI action map.")]
        private InputActionAsset m_InputActions;

        [SerializeField, Tooltip("Color palette used to tint the HUD active color indicator.")]
        private SO_ColorPalette m_Palette;

        [Header("Panels")]
        [SerializeField, Tooltip("In-run HUD panel.")]
        private GameObject m_HUDPanel;

        [SerializeField, Tooltip("Main menu panel.")]
        private GameObject m_MainMenuPanel;

        [SerializeField, Tooltip("Pause panel.")]
        private GameObject m_PausePanel;

        [SerializeField, Tooltip("Game over panel.")]
        private GameObject m_GameOverPanel;

        [SerializeField, Tooltip("Options panel (overlay).")]
        private GameObject m_OptionsPanel;

        [SerializeField, Tooltip("Controls panel (overlay).")]
        private GameObject m_ControlsPanel;

        [SerializeField, Tooltip("Credits panel (overlay).")]
        private GameObject m_CreditsPanel;

        [Header("HUD")]
        [SerializeField, Tooltip("Score text.")]
        private TextMeshProUGUI m_ScoreText;

        [SerializeField, Tooltip("Distance text.")]
        private TextMeshProUGUI m_DistanceText;

        [SerializeField, Tooltip("Image that shows the active color.")]
        private Image m_ActiveColorImage;

        [SerializeField, Tooltip("Icon shown while the shield is active.")]
        private GameObject m_ShieldIcon;

        [SerializeField, Tooltip("Icon shown while a boost is active.")]
        private GameObject m_BoostIcon;

        [SerializeField, Tooltip("Fill that shows the remaining boost time.")]
        private Image m_BoostFill;

        [SerializeField, Tooltip("Pause button on the HUD.")]
        private Button m_PauseButton;

        [Header("Main Menu")]
        [SerializeField, Tooltip("Play button.")]
        private Button m_PlayButton;

        [SerializeField, Tooltip("Options button.")]
        private Button m_OptionsButton;

        [SerializeField, Tooltip("Credits button.")]
        private Button m_CreditsButton;

        [SerializeField, Tooltip("Quit button.")]
        private Button m_QuitButton;

        [Header("Pause")]
        [SerializeField, Tooltip("Resume button.")]
        private Button m_ResumeButton;

        [SerializeField, Tooltip("Options button on the pause panel.")]
        private Button m_PauseOptionsButton;

        [SerializeField, Tooltip("Back to menu button on the pause panel.")]
        private Button m_PauseMenuButton;

        [Header("Game Over")]
        [SerializeField, Tooltip("Final score text.")]
        private TextMeshProUGUI m_FinalScoreText;

        [SerializeField, Tooltip("Best score text.")]
        private TextMeshProUGUI m_BestScoreText;

        [SerializeField, Tooltip("New record label, visible only when the record is beaten.")]
        private GameObject m_NewRecordText;

        [SerializeField, Tooltip("Retry button.")]
        private Button m_RetryButton;

        [SerializeField, Tooltip("Back to menu button on the game over panel.")]
        private Button m_GameOverMenuButton;

        [Header("Options")]
        [SerializeField, Tooltip("Master volume slider (0-100).")]
        private Slider m_MasterSlider;

        [SerializeField, Tooltip("Music volume slider (0-100).")]
        private Slider m_MusicSlider;

        [SerializeField, Tooltip("SFX volume slider (0-100).")]
        private Slider m_SfxSlider;

        [SerializeField, Tooltip("Mute toggle.")]
        private Toggle m_MuteToggle;

        [SerializeField, Tooltip("Controls button on the options panel.")]
        private Button m_ControlsButton;

        [SerializeField, Tooltip("Back button on the options panel.")]
        private Button m_OptionsBackButton;

        [Header("Controls")]
        [SerializeField, Tooltip("Back button on the controls panel.")]
        private Button m_ControlsBackButton;

        [Header("Credits")]
        [SerializeField, Tooltip("Credits text.")]
        private TextMeshProUGUI m_CreditsText;

        [SerializeField, Tooltip("Skip button on the credits panel.")]
        private Button m_CreditsSkipButton;

        #endregion


        #region FIELDS

        private IGameManager m_GameManager;
        private ISceneLoader m_SceneLoader;
        private ISaveService m_SaveService;
        private IAudioService m_AudioService;
        private ScoreManager m_ScoreManager;
        private RunManager m_RunManager;
        private GameFlow m_GameFlow;

        private InputAction m_PauseAction;
        private InputAction m_ConfirmAction;
        private InputAction m_CancelAction;

        /// <summary>Whether the run that just ended beat the record.</summary>
        private bool m_IsNewRecord;

        /// <summary>Panel of the current game state (the base screen).</summary>
        private UIPanel m_StatePanel = UIPanel.MainMenu;

        /// <summary>
        /// Overlay currently shown on top of the base screen. Never a base
        /// panel: <see cref="UIPanel.HUD"/> means "no overlay".
        /// </summary>
        private UIPanel m_OverlayPanel = UIPanel.HUD;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_GameManager = ServiceLocator.Current != null ? ServiceLocator.Current.Get<IGameManager>() : null;
            m_SceneLoader = ServiceLocator.Current != null ? ServiceLocator.Current.Get<ISceneLoader>() : null;
            m_SaveService = ServiceLocator.Current != null ? ServiceLocator.Current.Get<ISaveService>() : null;
            m_AudioService = ServiceLocator.Current != null ? ServiceLocator.Current.Get<IAudioService>() : null;

            ResolveActions();
            BindButtons();
        }

        private void OnEnable()
        {
            if (m_GameManager != null)
            {
                m_GameManager.StateChanged += HandleStateChanged;

                // The game can already be in a state (MainMenu on boot, or a
                // reloaded scene in any other state). No StateChanged event is
                // raised for the current state, so the panel has to be applied
                // once here — otherwise the UI loads and stays invisible.
                HandleStateChanged(m_GameManager.CurrentState);
            }

            ResolveSceneSystems();
        }

        private void OnDisable()
        {
            if (m_GameManager != null)
            {
                m_GameManager.StateChanged -= HandleStateChanged;
            }

            UnsubscribeSceneSystems();
        }

        private void Update()
        {
            ReadUIInput();
        }

        #endregion


        #region PUBLIC METHODS

        /// <inheritdoc />
        public void ShowPanel(UIPanel panel)
        {
            // A state change always closes any overlay: the overlay belongs to
            // the panel that opened it, and the state moved on.
            m_OverlayPanel = UIPanel.HUD;
            m_StatePanel = panel;
            ApplyPanelVisibility();

            // Clear the previous selection so the new panel does not inherit a
            // stale selected GameObject from the panel that was just hidden.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// Applies which panels are visible: the one for the current game state,
        /// plus the overlay when there is one. Overlays are children of the same
        /// Canvas, so both can be visible at the same time.
        /// </summary>
        private void ApplyPanelVisibility()
        {
            if (m_HUDPanel != null) m_HUDPanel.SetActive(m_StatePanel == UIPanel.HUD);
            if (m_MainMenuPanel != null) m_MainMenuPanel.SetActive(m_StatePanel == UIPanel.MainMenu);
            if (m_PausePanel != null) m_PausePanel.SetActive(m_StatePanel == UIPanel.Pause);
            if (m_GameOverPanel != null) m_GameOverPanel.SetActive(m_StatePanel == UIPanel.GameOver);

            if (m_OptionsPanel != null) m_OptionsPanel.SetActive(m_OverlayPanel == UIPanel.Options);
            if (m_ControlsPanel != null) m_ControlsPanel.SetActive(m_OverlayPanel == UIPanel.Controls);
            if (m_CreditsPanel != null) m_CreditsPanel.SetActive(m_OverlayPanel == UIPanel.Credits);
        }

        /// <inheritdoc />
        public void GoToMenu()
        {
            GameFlow()?.GoToMenu();
        }

        /// <inheritdoc />
        public void ShowGameOver(int score, int bestScore, bool isNewRecord)
        {
            if (m_FinalScoreText != null) m_FinalScoreText.text = score.ToString();
            if (m_BestScoreText != null) m_BestScoreText.text = bestScore.ToString();
            if (m_NewRecordText != null) m_NewRecordText.SetActive(isNewRecord);
        }

        /// <inheritdoc />
        public void SetHudScore(int score)
        {
            if (m_ScoreText != null) m_ScoreText.text = score.ToString();
        }

        /// <inheritdoc />
        public void SetHudDistance(float distance)
        {
            if (m_DistanceText != null) m_DistanceText.text = $"{Mathf.FloorToInt(distance)} m";
        }

        /// <inheritdoc />
        public void SetHudActiveColor(ColorId color)
        {
            if (m_ActiveColorImage == null || m_Palette == null)
            {
                return;
            }

            m_ActiveColorImage.color = m_Palette.GetColor(color);
        }

        /// <inheritdoc />
        public void SetHudShield(bool active)
        {
            if (m_ShieldIcon != null) m_ShieldIcon.SetActive(active);
        }

        /// <inheritdoc />
        public void SetHudBoost(bool active, float normalizedTimeLeft)
        {
            if (m_BoostIcon != null) m_BoostIcon.SetActive(active);

            if (m_BoostFill != null)
            {
                m_BoostFill.fillAmount = Mathf.Clamp01(normalizedTimeLeft);
            }
        }

        /// <inheritdoc />
        public void ApplySettings(SettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            if (m_MasterSlider != null) m_MasterSlider.value = PercentFromNormalized(settings.MasterVolume);
            if (m_MusicSlider != null) m_MusicSlider.value = PercentFromNormalized(settings.MusicVolume);
            if (m_SfxSlider != null) m_SfxSlider.value = PercentFromNormalized(settings.SfxVolume);
            if (m_MuteToggle != null) m_MuteToggle.isOn = settings.IsMuted;
        }

        /// <inheritdoc />
        public SettingsData CollectSettings()
        {
            SettingsData result = m_SaveService != null ? m_SaveService.GetSettings() : new SettingsData();

            if (m_MasterSlider != null) result.MasterVolume = NormalizedFromPercent(m_MasterSlider.value);
            if (m_MusicSlider != null) result.MusicVolume = NormalizedFromPercent(m_MusicSlider.value);
            if (m_SfxSlider != null) result.SfxVolume = NormalizedFromPercent(m_SfxSlider.value);
            if (m_MuteToggle != null) result.IsMuted = m_MuteToggle.isOn;

            return result;
        }

        #endregion


        #region PRIVATE METHODS

        private void ResolveActions()
        {
            if (m_InputActions == null)
            {
                return;
            }

            m_PauseAction = UIInput.Find(m_InputActions, UIInput.c_Pause);
            m_ConfirmAction = UIInput.Find(m_InputActions, UIInput.c_Confirm);
            m_CancelAction = UIInput.Find(m_InputActions, UIInput.c_Cancel);

            if (m_PauseAction != null) m_PauseAction.Enable();
            if (m_ConfirmAction != null) m_ConfirmAction.Enable();
            if (m_CancelAction != null) m_CancelAction.Enable();
        }

        private void BindButtons()
        {
            if (m_PauseButton != null) m_PauseButton.onClick.AddListener(() => GameFlow()?.Pause());
            if (m_PlayButton != null) m_PlayButton.onClick.AddListener(StartGame);
            if (m_OptionsButton != null) m_OptionsButton.onClick.AddListener(ShowOptions);
            if (m_CreditsButton != null) m_CreditsButton.onClick.AddListener(() => ShowOverlay(UIPanel.Credits));
            if (m_QuitButton != null) m_QuitButton.onClick.AddListener(QuitGame);

            if (m_ResumeButton != null) m_ResumeButton.onClick.AddListener(() => GameFlow()?.Resume());
            if (m_PauseOptionsButton != null) m_PauseOptionsButton.onClick.AddListener(ShowOptions);
            if (m_PauseMenuButton != null) m_PauseMenuButton.onClick.AddListener(() => GameFlow()?.GoToMenu());

            if (m_RetryButton != null) m_RetryButton.onClick.AddListener(() => GameFlow()?.Restart());
            if (m_GameOverMenuButton != null) m_GameOverMenuButton.onClick.AddListener(() => GameFlow()?.GoToMenu());

            if (m_ControlsButton != null) m_ControlsButton.onClick.AddListener(() => ShowOverlay(UIPanel.Controls));
            if (m_OptionsBackButton != null) m_OptionsBackButton.onClick.AddListener(ApplyAndCloseOptions);
            if (m_ControlsBackButton != null) m_ControlsBackButton.onClick.AddListener(ShowOptions);

            if (m_CreditsSkipButton != null) m_CreditsSkipButton.onClick.AddListener(() => GameFlow()?.GoToMenu());

            BindOptionsInputs();
        }

        private void BindOptionsInputs()
        {
            if (m_MasterSlider != null) m_MasterSlider.onValueChanged.AddListener(_ => ApplyOptionsToAudio());
            if (m_MusicSlider != null) m_MusicSlider.onValueChanged.AddListener(_ => ApplyOptionsToAudio());
            if (m_SfxSlider != null) m_SfxSlider.onValueChanged.AddListener(_ => ApplyOptionsToAudio());
            if (m_MuteToggle != null) m_MuteToggle.onValueChanged.AddListener(_ => ApplyOptionsToAudio());
        }

        /// <summary>
        /// Reacts to the global game state: exactly one panel is visible per
        /// state, and the HUD subscriptions follow it.
        /// This runs while the UIManager is still enabled but the scene systems
        /// of the previous run may already be destroyed, so every non-Playing
        /// state unsubscribes BEFORE touching the panels: Unity compares a
        /// destroyed object to null, and re-subscribing to those dead references
        /// would silently kill the HUD on the next run.
        /// </summary>
        /// <param name="state">New global game state.</param>
        private void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    UnsubscribeSceneSystems();
                    ShowPanel(UIPanel.MainMenu);
                    break;
                case GameState.Playing:
                    // Resolve first: the Game scene (and its ScoreManager/
                    // RunManager) may be a different instance from the last run.
                    ResolveSceneSystems();
                    ShowPanel(UIPanel.HUD);
                    SubscribeSceneSystems();
                    break;
                case GameState.Paused:
                    UnsubscribeSceneSystems();
                    ShowPanel(UIPanel.Pause);
                    break;
                case GameState.GameOver:
                    UnsubscribeSceneSystems();
                    ShowPanel(UIPanel.GameOver);
                    break;
            }
        }

        private void ResolveSceneSystems()
        {
            // Object == null is true for BOTH a missing object and a destroyed
            // one, so this is also what clears the dead references left behind
            // when the Game scene is unloaded.
            m_ScoreManager = Object.FindObjectOfType<ScoreManager>();
            m_RunManager = Object.FindObjectOfType<RunManager>();
            m_GameFlow = Object.FindObjectOfType<GameFlow>();
        }

        private void SubscribeSceneSystems()
        {
            // A new run starts from zero, so the previous run's record no longer applies.
            m_IsNewRecord = false;

            if (m_ScoreManager != null)
            {
                m_ScoreManager.OnScoreChanged += SetHudScore;
                m_ScoreManager.OnColorChanged += SetHudActiveColor;
                m_ScoreManager.OnShieldChanged += SetHudShield;
                m_ScoreManager.OnBoostChanged += HandleBoostChanged;
                m_ScoreManager.OnPlayerDied += HandlePlayerDied;
                m_ScoreManager.OnRecordBeaten += HandleRecordBeaten;
            }

            if (m_RunManager != null)
            {
                m_RunManager.OnDistanceChanged += SetHudDistance;
            }
        }

        private void UnsubscribeSceneSystems()
        {
            if (m_ScoreManager != null)
            {
                m_ScoreManager.OnScoreChanged -= SetHudScore;
                m_ScoreManager.OnColorChanged -= SetHudActiveColor;
                m_ScoreManager.OnShieldChanged -= SetHudShield;
                m_ScoreManager.OnBoostChanged -= HandleBoostChanged;
                m_ScoreManager.OnPlayerDied -= HandlePlayerDied;
                m_ScoreManager.OnRecordBeaten -= HandleRecordBeaten;
            }

            if (m_RunManager != null)
            {
                m_RunManager.OnDistanceChanged -= SetHudDistance;
            }
        }

        /// <summary>
        /// Reads the UI actions (Esc pause/resume, Enter/E confirm, Esc cancel)
        /// from the UI action map. When the EventSystem has a selected
        /// GameObject the Confirm/Cancel inputs are dispatched to it (submit /
        /// cancel handlers), so buttons react to keyboard and gamepad. With no
        /// selection, the inputs fall back to the panel-level defaults:
        /// MainMenu Confirm starts the game, Paused Cancel resumes, and
        /// GameOver Confirm restarts.
        /// </summary>
        private void ReadUIInput()
        {
            if (m_PauseAction != null && m_PauseAction.WasPressedThisFrame())
            {
                if (m_GameManager != null && m_GameManager.CurrentState == GameState.Playing)
                {
                    GameFlow()?.Pause();
                }
                else if (m_GameManager != null && m_GameManager.CurrentState == GameState.Paused)
                {
                    GameFlow()?.Resume();
                }
            }

            HandleConfirmInput();
            HandleCancelInput();
        }

        private void HandleConfirmInput()
        {
            if (m_ConfirmAction == null || !m_ConfirmAction.WasPressedThisFrame())
            {
                return;
            }

            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            if (selected != null)
            {
                EventSystem.current.SetSelectedGameObject(selected);
                ExecuteEvents.Execute(selected, null, ExecuteEvents.submitHandler);
                return;
            }

            if (m_OverlayPanel != UIPanel.HUD)
            {
                // An overlay (Options/Controls/Credits) owns the keyboard while
                // it is open: the fallback must not start the game underneath it.
                return;
            }

            if (m_GameManager == null)
            {
                return;
            }

            switch (m_GameManager.CurrentState)
            {
                case GameState.MainMenu:
                    StartGame();
                    break;
                case GameState.GameOver:
                    GameFlow()?.Restart();
                    break;
            }
        }

        private void HandleCancelInput()
        {
            if (m_CancelAction == null || !m_CancelAction.WasPressedThisFrame())
            {
                return;
            }

            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            if (selected != null)
            {
                ExecuteEvents.Execute(selected, null, ExecuteEvents.cancelHandler);
                return;
            }

            if (m_OverlayPanel != UIPanel.HUD)
            {
                return;
            }

            if (m_GameManager != null && m_GameManager.CurrentState == GameState.Paused)
            {
                GameFlow()?.Resume();
            }
        }

        private void StartGame()
        {
            GameFlow()?.StartGame();
        }

        private void ShowOptions()
        {
            if (m_SaveService != null)
            {
                ApplySettings(m_SaveService.GetSettings());
            }

            ShowOverlay(UIPanel.Options);
        }

        private void ApplyAndCloseOptions()
        {
            ApplyOptionsToAudio();
            CloseOverlay();
        }

        /// <summary>
        /// Shows an overlay (Options, Controls, Credits) on top of the panel the
        /// game is already in, so returning from it restores the previous screen.
        /// </summary>
        /// <param name="overlay">Overlay to show.</param>
        private void ShowOverlay(UIPanel overlay)
        {
            m_OverlayPanel = overlay;
            ApplyPanelVisibility();
        }

        /// <summary>
        /// Hides the current overlay and returns to the panel of the game state.
        /// </summary>
        private void CloseOverlay()
        {
            m_OverlayPanel = UIPanel.HUD;
            ApplyPanelVisibility();
        }

        private void ApplyOptionsToAudio()
        {
            SettingsData settings = CollectSettings();

            m_SaveService?.SetSettings(settings);
            m_SaveService?.Save();

            m_AudioService?.SetMasterVolume(settings.MasterVolume);
            m_AudioService?.SetMusicVolume(settings.MusicVolume);
            m_AudioService?.SetSfxVolume(settings.SfxVolume);
            m_AudioService?.SetMuted(settings.IsMuted);
        }

        /// <summary>
        /// TODO(Task 7 review): the ScoreManager publishes only OnBoostChanged
        /// (colors), with no per-frame remaining-time event. BoostTimeLeft/
        /// BoostDuration exist as properties, so the fill can be driven from
        /// Update() while a boost is active instead of being pinned at 1. The
        /// HUD icon state still comes from this event.
        /// </summary>
        /// <param name="colors">Colors covered by the boost (empty when it ended).</param>
        private void HandleBoostChanged(System.Collections.Generic.List<ColorId> colors)
        {
            bool active = colors != null && colors.Count > 0;
            SetHudBoost(active, active ? 1f : 0f);
        }

        /// <summary>
        /// TODO(Task 7 review): the new-record flag is unreliable here. ScoreManager.Die()
        /// updates m_BestScore BEFORE raising OnPlayerDied, so by the time this
        /// runs Score == BestScore even when the record was NOT beaten, making
        /// the condition below true for every run with points. A dedicated
        /// "record beaten" event (or the value returned by Die/CloseRun) is
        /// needed to publish the label correctly.
        /// </summary>
        private void HandlePlayerDied()
        {
            if (m_ScoreManager == null)
            {
                return;
            }

            ShowGameOver(m_ScoreManager.Score, m_ScoreManager.BestScore, m_IsNewRecord);
        }

        /// <summary>
        /// Remembers that the run beat the record. Published before
        /// <c>OnPlayerDied</c>, so comparing the scores afterwards would always
        /// be true.
        /// </summary>
        /// <param name="newRecord">The new record.</param>
        private void HandleRecordBeaten(int newRecord)
        {
            m_IsNewRecord = true;
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private GameFlow GameFlow()
        {
            if (m_GameFlow == null)
            {
                m_GameFlow = Object.FindObjectOfType<GameFlow>();
            }

            return m_GameFlow;
        }

        private float NormalizedFromPercent(float percent)
        {
            return Mathf.Clamp01(percent / 100f);
        }

        private float PercentFromNormalized(float normalized)
        {
            return Mathf.Clamp01(normalized) * 100f;
        }

        #endregion
    }
}
