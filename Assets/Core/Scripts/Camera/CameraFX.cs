using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using PowerPulse.Core;
using PowerPulse.Obstacles;
using PowerPulse.Player;
using PowerPulse.VFX;

namespace PowerPulse.CameraSystem
{
    /// <summary>
    /// Owns every juice effectt of the run: camera shake, FOV punch, full-screen
    /// flash, temporary Bloom/Chromatic Aberration adjustments and the pooled VFX
    /// spawned on collection, death, dash, portal and power-up events. It lives on
    /// the Main Camera, right after the <see cref="CameraFollow"/>, and never
    /// decides gameplay rules: it only reacts to the events published by the run.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class CameraFX : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("Config")]
        [SerializeField, Tooltip("VFX settings asset with every intensity, duration and prefab.")]
        private Config_VFX m_Config;

        [Header("References")]
        [SerializeField, Tooltip("Camera that receives the effects. Defaults to the camera on this GameObject.")]
        private Camera m_Camera;

        [SerializeField, Tooltip("Camera follow that owns the base position. Defaults to the component on this GameObject.")]
        private CameraFollow m_Follow;

        [SerializeField, Tooltip("Full-screen flash image. Leave empty to create a runtime overlay.")]
        private Image m_FlashImage;

        [SerializeField, Tooltip("Global volume that owns Bloom and Chromatic Aberration. Found automatically when empty.")]
        private Volume m_Volume;

        [SerializeField, Tooltip("Player transform, used as the position of the death and dash VFX.")]
        private Transform m_Player;

        [SerializeField, Tooltip("Color palette used to tint the VFX. Falls back to the ScoreManager palette.")]
        private SO_ColorPalette m_Palette;

        [SerializeField, Tooltip("Shared object pool used to recycle the VFX. Found automatically when empty.")]
        private ObjectPool m_Pool;

        [SerializeField, Tooltip("Score manager that publishes the run events. Found automatically when empty.")]
        private ScoreManager m_ScoreManager;

        [SerializeField, Tooltip("Spawn manager that publishes portal and power-up events. Found automatically when empty.")]
        private SpawnManager m_SpawnManager;

        [SerializeField, Tooltip("Player controller that publishes the dash event. Found automatically when empty.")]
        private PlayerController m_PlayerController;

        #endregion


        #region NESTED TYPES

        /// <summary>A VFX instance currently playing and its remaining lifetime.</summary>
        private class ActiveVfx
        {
            public GameObject Prefab;
            public GameObject Instance;
            public float Timer;
            public bool UseUnscaledTime;
        }

        #endregion


        #region FIELDS

        private VolumeProfile m_Profile;
        private Bloom m_Bloom;
        private ChromaticAberration m_ChromaticAberration;
        private float m_BaseBloom;
        private float m_BaseChromaticAberration;
        private float m_CurrentBloom;
        private float m_CurrentChromaticAberration;

        private float m_BaseFov;
        private float m_ShakeTimer;
        private float m_ShakeDuration;
        private float m_ShakeAmplitude;
        private float m_ShakeFrequency;
        private float m_ShakeElapsed;
        private Vector3 m_ShakeOffset;

        private float m_FlashTimer;
        private float m_FlashDuration;
        private float m_FlashAlpha;
        private Color m_FlashColor;

        private bool m_BoostActive;
        private float m_BoostTimeLeft;
        private float m_BoostDuration;
        private float m_DeathPostTimer;

        private readonly List<ActiveVfx> m_ActiveVfx = new List<ActiveVfx>();
        private Canvas m_RuntimeCanvas;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            ResolveReferences();
            ResolvePostProcessing();
            PrewarmVfx();

            m_BaseFov = m_Camera != null ? m_Camera.fieldOfView : 60f;

            if (m_Config == null)
            {
                Debug.LogError("[CameraFX] The VFX settings asset is not assigned. The effects will not run.", this);
            }
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            ClearVfx();
            RestorePostProcessing();
        }

        private void OnDestroy()
        {
            RestorePostProcessing();
        }

        private void LateUpdate()
        {
            TickShake(Time.deltaTime);
            TickFov(Time.deltaTime);
            TickFlash(Time.deltaTime);
            TickPostProcessing(Time.deltaTime);
            TickVfx(Time.deltaTime);

            // The follow owns the base position; the shake is an additive offset
            // applied after it, so the two never fight for the transform.
            if (m_Follow != null)
            {
                transform.position = m_Follow.FollowPosition + m_ShakeOffset;
            }
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Triggers the collect feedback: VFX tinted with the collected color and a
        /// light camera shake.
        /// </summary>
        /// <param name="color">Color of the collected obstacle.</param>
        /// <param name="position">World position of the collection.</param>
        public void PlayCollect(Color color, Vector3 position)
        {
            if (m_Config == null)
            {
                return;
            }

            PlayVfx(m_Config.CollectVfx, position, color, m_Config.CollectPrewarm, m_Config.VfxScale);
            Shake(m_Config.CollectShakeAmplitude, m_Config.CollectShakeDuration, m_Config.CollectShakeFrequency);
        }

        /// <summary>
        /// Triggers the death feedback: VFX, strong shake, flash and a temporary
        /// Chromatic Aberration boost.
        /// </summary>
        public void PlayDeath(Vector3 position, Color color)
        {
            if (m_Config == null)
            {
                return;
            }

            // Single death VFX (particles + screen fx combined in one prefab)
            // useUnscaledTime = true so particles persist frozen at timeScale=0 if config says so
            PlayVfx(m_Config.DeathVfx, position, color, m_Config.DeathPrewarm, m_Config.VfxScale, m_Config.DeathVfxPersistOnGameOver);
            Shake(m_Config.DeathShakeAmplitude, m_Config.DeathShakeDuration, m_Config.DeathShakeFrequency);
            Flash(m_Config.FlashColor, m_Config.FlashAlpha, m_Config.DeathFlashDuration);

            m_DeathPostTimer = m_Config.DeathPostFxDuration;
        }

        /// <summary>
        /// Triggers the portal feedback: VFX tinted with the portal color and a
        /// short flash.
        /// </summary>
        /// <param name="color">Color of the portal.</param>
        /// <param name="position">World position of the portal.</param>
        public void PlayPortal(Color color, Vector3 position)
        {
            if (m_Config == null)
            {
                return;
            }

            PlayVfx(m_Config.PortalVfx, position, color, m_Config.PortalPrewarm, m_Config.VfxScale);
            Flash(m_Config.FlashColor, m_Config.FlashAlpha, m_Config.PortalFlashDuration);
        }

        /// <summary>
        /// Triggers the dash feedback: VFX tinted with the active color.
        /// </summary>
        /// <param name="position">World position of the player.</param>
        public void PlayDash(Vector3 position)
        {
            if (m_Config == null)
            {
                return;
            }

            Color color = GetPaletteColor(m_ScoreManager != null ? m_ScoreManager.ActiveColor : ColorId.Red);
            PlayVfx(m_Config.DashVfx, position, color, m_Config.DashPrewarm, m_Config.VfxScale);
        }

        #endregion


        #region EVENT HANDLERS

        private void HandleCollected(ColorId color, int points, Vector3 position)
        {
            PlayCollect(GetPaletteColor(color), position);
        }

        private void HandlePlayerDied(Obstacle killingObstacle)
        {
            if (m_ScoreManager == null)
            {
                PlayDeath(transform.position, Color.white);
                return;
            }
            Color color = GetPaletteColor(m_ScoreManager.ActiveColor);
            Vector3 position = m_Player != null ? m_Player.position : transform.position;
            PlayDeath(position, color);
        }

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
            }
        }

        private void HandleRunReset()
        {
            ClearVfx();
            m_ShakeTimer = 0f;
            m_ShakeOffset = Vector3.zero;
            m_FlashTimer = 0f;
            m_BoostActive = false;
            m_DeathPostTimer = 0f;

            if (m_Camera != null)
            {
                m_Camera.fieldOfView = m_BaseFov;
            }

            if (m_FlashImage != null)
            {
                m_FlashImage.color = new Color(m_FlashColor.r, m_FlashColor.g, m_FlashColor.b, 0f);
            }

            RestorePostProcessing();
        }

        private void HandlePortalCrossed(ColorId color, Vector3 position)
        {
            PlayPortal(GetPaletteColor(color), position);
        }

        private void HandlePowerUpCollected(SO_PowerUpCatalog.PowerUpEntry entry, Vector3 position)
        {
            if (m_Config == null)
            {
                return;
            }

            GameObject prefab = entry != null ? entry.VfxPrefab : null;

            if (prefab == null)
            {
                prefab = m_Config.PowerUpVfx;
            }

            Color color = GetPowerUpColor(entry);
            PlayVfx(prefab, position, color, m_Config.PowerUpPrewarm, m_Config.VfxScale);
        }

        private void HandleDashed(int direction)
        {
            Vector3 position = m_Player != null ? m_Player.position : transform.position;
            PlayDash(position);
        }

        #endregion


        #region PRIVATE METHODS

        // ===== REFERENCES =====

        /// <summary>
        /// Fills every optional reference from the scene, so the Inspector only
        /// needs the config asset in the common case.
        /// </summary>
        private void ResolveReferences()
        {
            if (m_Camera == null)
            {
                m_Camera = GetComponent<Camera>();
            }

            if (m_Follow == null)
            {
                m_Follow = GetComponent<CameraFollow>();
            }

            if (m_Volume == null)
            {
                m_Volume = FindAnyObjectByType<Volume>();
            }

            if (m_Player == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                m_Player = player != null ? player.transform : null;
            }

            if (m_Pool == null)
            {
                m_Pool = FindAnyObjectByType<ObjectPool>();
            }

            if (m_ScoreManager == null)
            {
                m_ScoreManager = FindAnyObjectByType<ScoreManager>();
            }

            if (m_SpawnManager == null)
            {
                m_SpawnManager = FindAnyObjectByType<SpawnManager>();
            }

            if (m_PlayerController == null && m_Player != null)
            {
                m_PlayerController = m_Player.GetComponent<PlayerController>();
            }

            if (m_Palette == null && m_ScoreManager != null)
            {
                m_Palette = m_ScoreManager.Palette;
            }
        }

        /// <summary>
        /// Reads Bloom and Chromatic Aberration from the global volume and stores
        /// their base values, so the effects can be restored later.
        /// </summary>
        private void ResolvePostProcessing()
        {
            if (m_Volume == null)
            {
                return;
            }

            m_Profile = m_Volume.sharedProfile;

            if (m_Profile == null)
            {
                return;
            }

            if (m_Profile.TryGet(out Bloom bloom))
            {
                m_Bloom = bloom;
                m_BaseBloom = bloom.intensity.value;
                m_CurrentBloom = m_BaseBloom;
            }

            if (m_Profile.TryGet(out ChromaticAberration chromaticAberration))
            {
                m_ChromaticAberration = chromaticAberration;
                m_BaseChromaticAberration = chromaticAberration.intensity.value;
                m_CurrentChromaticAberration = m_BaseChromaticAberration;
            }
        }

        /// <summary>
        /// Pre-creates the VFX pools so gameplay never instantiates them.
        /// </summary>
        private void PrewarmVfx()
        {
            if (m_Pool == null || m_Config == null)
            {
                return;
            }

            Prewarm(m_Config.CollectVfx, m_Config.CollectPrewarm);
            Prewarm(m_Config.DeathVfx, m_Config.DeathPrewarm);
            Prewarm(m_Config.DeathObstacleExplosionVfx, m_Config.DeathPrewarm);
            Prewarm(m_Config.DashVfx, m_Config.DashPrewarm);
            Prewarm(m_Config.PortalVfx, m_Config.PortalPrewarm);
            Prewarm(m_Config.PowerUpVfx, m_Config.PowerUpPrewarm);
        }

        /// <summary>
        /// Creates the pool of a single VFX prefab.
        /// </summary>
        /// <param name="prefab">Prefab to prewarm.</param>
        /// <param name="prewarm">How many instances to create.</param>
        private void Prewarm(GameObject prefab, int prewarm)
        {
            if (prefab == null || prewarm <= 0)
            {
                return;
            }

            m_Pool.CreatePool(prefab, prewarm);
        }

        /// <summary>
        /// Subscribes to every event the effects react to.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (m_ScoreManager != null)
            {
                m_ScoreManager.OnCollected += HandleCollected;
                m_ScoreManager.OnPlayerDied += HandlePlayerDied;
                m_ScoreManager.OnBoostChanged += HandleBoostChanged;
                m_ScoreManager.OnRunReset += HandleRunReset;
            }

            if (m_SpawnManager != null)
            {
                m_SpawnManager.OnPortalCrossed += HandlePortalCrossed;
                m_SpawnManager.OnPowerUpCollected += HandlePowerUpCollected;
            }

            if (m_PlayerController != null)
            {
                m_PlayerController.OnDashed += HandleDashed;
            }
        }

        /// <summary>
        /// Removes every subscription (mirror of <see cref="SubscribeToEvents"/>).
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (m_ScoreManager != null)
            {
                m_ScoreManager.OnCollected -= HandleCollected;
                m_ScoreManager.OnPlayerDied -= HandlePlayerDied;
                m_ScoreManager.OnBoostChanged -= HandleBoostChanged;
                m_ScoreManager.OnRunReset -= HandleRunReset;
            }

            if (m_SpawnManager != null)
            {
                m_SpawnManager.OnPortalCrossed -= HandlePortalCrossed;
                m_SpawnManager.OnPowerUpCollected -= HandlePowerUpCollected;
            }

            if (m_PlayerController != null)
            {
                m_PlayerController.OnDashed -= HandleDashed;
            }
        }

        // ===== SHAKE =====

        /// <summary>
        /// Starts a camera shake. A new shake replaces the current one.
        /// </summary>
        /// <param name="amplitude">Offset amplitude in world units.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="frequency">Noise frequency.</param>
        private void Shake(float amplitude, float duration, float frequency)
        {
            m_ShakeAmplitude = Mathf.Max(0f, amplitude);
            m_ShakeDuration = Mathf.Max(0.01f, duration);
            m_ShakeFrequency = Mathf.Max(0.01f, frequency);
            m_ShakeTimer = m_ShakeDuration;
            m_ShakeElapsed = 0f;
        }

        /// <summary>
        /// Updates the shake offset with a smooth Perlin noise and a linear fade.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        private void TickShake(float deltaTime)
        {
            if (m_ShakeTimer <= 0f)
            {
                m_ShakeOffset = Vector3.zero;
                return;
            }

            m_ShakeTimer -= deltaTime;
            m_ShakeElapsed += deltaTime;

            float fade = Mathf.Clamp01(m_ShakeTimer / m_ShakeDuration);
            float time = m_ShakeElapsed * m_ShakeFrequency;

            m_ShakeOffset = new Vector3(
                Mathf.PerlinNoise(time, 0f) - 0.5f,
                Mathf.PerlinNoise(0f, time) - 0.5f,
                0f) * 2f * m_ShakeAmplitude * fade;
        }

        // ===== FOV =====

        /// <summary>
        /// Moves the field of view toward the boost target. Starts returning to
        /// normal before the boost expires to give the player a clear warning.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        private void TickFov(float deltaTime)
        {
            if (m_Camera == null || m_Config == null)
            {
                return;
            }

            float targetFov = m_BaseFov;

            if (m_BoostActive)
            {
                float returnTime = m_Config.BoostFovReturnTime;
                float progress = 0f;

                if (m_BoostDuration > 0f && returnTime > 0f)
                {
                    // How far into the "return phase" we are (0 = just entered return phase, 1 = boost expired)
                    progress = 1f - Mathf.Clamp01(m_BoostTimeLeft / returnTime);
                }

                if (progress > 0f)
                {
                    // In return phase: lerp from boosted FOV back to base FOV using the curve
                    float curveValue = m_Config.BoostReturnCurve.Evaluate(progress);
                    targetFov = Mathf.Lerp(m_BaseFov + m_Config.BoostFovPunch, m_BaseFov, curveValue);
                }
                else
                {
                    // Full boost phase
                    targetFov = m_BaseFov + m_Config.BoostFovPunch;
                }
            }

            m_Camera.fieldOfView = Mathf.Lerp(m_Camera.fieldOfView, targetFov, m_Config.FovSpeed * deltaTime);

            // Update boost time left for next frame
            if (m_BoostActive && m_BoostTimeLeft > 0f)
            {
                m_BoostTimeLeft -= deltaTime;
            }
        }

        // ===== FLASH =====

        /// <summary>
        /// Starts a full-screen flash that fades out over the given duration.
        /// </summary>
        /// <param name="color">Flash color.</param>
        /// <param name="alpha">Maximum alpha.</param>
        /// <param name="duration">Duration in seconds.</param>
        private void Flash(Color color, float alpha, float duration)
        {
            m_FlashColor = color;
            m_FlashAlpha = Mathf.Clamp01(alpha);
            m_FlashDuration = Mathf.Max(0.01f, duration);
            m_FlashTimer = m_FlashDuration;

            if (m_FlashImage == null)
            {
                EnsureFlashImage();
            }
        }

        /// <summary>
        /// Updates the flash alpha, fading out from the maximum to zero.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        private void TickFlash(float deltaTime)
        {
            if (m_FlashImage == null)
            {
                return;
            }

            if (m_FlashTimer > 0f)
            {
                m_FlashTimer -= deltaTime;
                float alpha = m_FlashAlpha * Mathf.Clamp01(m_FlashTimer / m_FlashDuration);
                m_FlashImage.color = new Color(m_FlashColor.r, m_FlashColor.g, m_FlashColor.b, alpha);
            }
            else
            {
                m_FlashImage.color = new Color(m_FlashColor.r, m_FlashColor.g, m_FlashColor.b, 0f);
            }
        }

        /// <summary>
        /// Creates a runtime screen overlay with a full-screen flash image, used
        /// when the scene has no Canvas assigned.
        /// </summary>
        private void EnsureFlashImage()
        {
            if (m_FlashImage != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("Screen Overlay (Runtime)");
            m_RuntimeCanvas = canvasObject.AddComponent<Canvas>();
            m_RuntimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject imageObject = new GameObject("Flash");
            imageObject.transform.SetParent(canvasObject.transform, false);

            m_FlashImage = imageObject.AddComponent<Image>();
            m_FlashImage.color = new Color(1f, 1f, 1f, 0f);
            m_FlashImage.raycastTarget = false;

            RectTransform rect = m_FlashImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // ===== POST-PROCESSING =====

        /// <summary>
        /// Moves Bloom and Chromatic Aberration toward their current targets and
        /// applies them to the shared profile.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        private void TickPostProcessing(float deltaTime)
        {
            if (m_Config == null || (m_Bloom == null && m_ChromaticAberration == null))
            {
                return;
            }

            if (m_DeathPostTimer > 0f)
            {
                m_DeathPostTimer -= deltaTime;
            }

            float bloomTarget = m_BaseBloom
                + (m_BoostActive ? m_Config.BoostBloomIntensity : 0f)
                + (m_DeathPostTimer > 0f ? m_Config.DeathBloomIntensity : 0f);

            float chromaticTarget = m_BaseChromaticAberration
                + (m_DeathPostTimer > 0f ? m_Config.DeathChromaticAberration : 0f);

            float step = m_Config.PostProcessingRestoreSpeed * deltaTime;

            if (m_Bloom != null)
            {
                m_CurrentBloom = Mathf.Lerp(m_CurrentBloom, bloomTarget, step);
                m_Bloom.intensity.value = m_CurrentBloom;
            }

            if (m_ChromaticAberration != null)
            {
                m_CurrentChromaticAberration = Mathf.Lerp(m_CurrentChromaticAberration, chromaticTarget, step);
                m_ChromaticAberration.intensity.value = m_CurrentChromaticAberration;
            }
        }

        /// <summary>
        /// Restores Bloom and Chromatic Aberration to the values read on Awake.
        /// </summary>
        private void RestorePostProcessing()
        {
            m_CurrentBloom = m_BaseBloom;
            m_CurrentChromaticAberration = m_BaseChromaticAberration;

            if (m_Bloom != null)
            {
                m_Bloom.intensity.value = m_BaseBloom;
            }

            if (m_ChromaticAberration != null)
            {
                m_ChromaticAberration.intensity.value = m_BaseChromaticAberration;
            }
        }

        // ===== VFX =====

        /// <summary>
        /// Spawns a VFX prefab at the given position, tints it and schedules its
        /// return to the pool. Public so other systems (e.g. PlayerDeath) can trigger VFX.
        /// </summary>
        /// <param name="prefab">VFX prefab.</param>
        /// <param name="position">World position.</param>
        /// <param name="color">Color used to tint the VFX.</param>
        /// <param name="prewarm">How many instances the pool keeps ready.</param>
        /// <param name="scale">Uniform scale of the instance.</param>
        /// <param name="useUnscaledTime">If true, timer advances on unscaled time (for death VFX that persist at timeScale=0).</param>
        public void PlayVfx(GameObject prefab, Vector3 position, Color color, int prewarm, float scale, bool useUnscaledTime = false)
        {
            if (prefab == null || m_Pool == null)
            {
                return;
            }

            m_Pool.CreatePool(prefab, prewarm);

            GameObject instance = m_Pool.Get(prefab, position, Quaternion.identity);

            if (instance == null)
            {
                return;
            }

            instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

            VFXClip clip = instance.GetComponent<VFXClip>();

            if (clip != null)
            {
                clip.Play(color);
            }

            float duration = clip != null ? clip.Duration : m_Config.DefaultVfxDuration;
            bool vfxUsesUnscaledTime = useUnscaledTime || (clip != null && clip.UseUnscaledTime);
            m_ActiveVfx.Add(new ActiveVfx { Prefab = prefab, Instance = instance, Timer = duration, UseUnscaledTime = vfxUsesUnscaledTime });
        }

        /// <summary>
        /// Advances every active VFX and recycles the ones that finished.
        /// </summary>
        /// <param name="deltaTime">Frame delta time (scaled).</param>
        private void TickVfx(float deltaTime)
        {
            float unscaledDelta = Time.unscaledDeltaTime;
            bool persistDeathVfx = m_Config != null && m_Config.DeathVfxPersistOnGameOver;

            for (int i = m_ActiveVfx.Count - 1; i >= 0; i--)
            {
                ActiveVfx vfx = m_ActiveVfx[i];
                float dt = vfx.UseUnscaledTime ? unscaledDelta : deltaTime;
                vfx.Timer -= dt;

                if (vfx.Timer <= 0f || vfx.Instance == null)
                {
                    // Don't recycle death VFX if persistOnGameOver is enabled and we're at timeScale=0 (Game Over screen)
                    if (persistDeathVfx && vfx.UseUnscaledTime && Time.timeScale == 0f)
                    {
                        continue; // Keep it alive, frozen on screen
                    }
                    ReleaseVfx(vfx);
                    m_ActiveVfx.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Returns every active VFX to its pool, used on reset and disable.
        /// </summary>
        private void ClearVfx()
        {
            for (int i = 0; i < m_ActiveVfx.Count; i++)
            {
                ReleaseVfx(m_ActiveVfx[i]);
            }

            m_ActiveVfx.Clear();
        }

        /// <summary>
        /// Stops and recycles a single VFX instance.
        /// </summary>
        /// <param name="vfx">VFX to release.</param>
        private void ReleaseVfx(ActiveVfx vfx)
        {
            if (vfx == null || vfx.Instance == null)
            {
                return;
            }

            VFXClip clip = vfx.Instance.GetComponent<VFXClip>();

            if (clip != null)
            {
                clip.Stop();
            }

            if (m_Pool != null)
            {
                m_Pool.Release(vfx.Prefab, vfx.Instance);
            }
        }

        // ===== COLORS =====

        /// <summary>
        /// Returns the palette color for a game color id, with a white fallback.
        /// Public so other systems can resolve colors.
        /// </summary>
        /// <param name="color">Game color id.</param>
        /// <returns>The palette color, or white when it cannot be resolved.</returns>
        public Color GetPaletteColor(ColorId color)
        {
            return m_Palette != null ? m_Palette.GetColor(color) : Color.white;
        }

        /// <summary>
        /// Returns a representative color for a power-up type, used to tint the
        /// power-up VFX.
        /// </summary>
        /// <param name="entry">Power-up entry (may be null).</param>
        /// <returns>The color used to tint the power-up VFX.</returns>
        private Color GetPowerUpColor(SO_PowerUpCatalog.PowerUpEntry entry)
        {
            if (entry == null)
            {
                return Color.white;
            }

            switch (entry.Type)
            {
                case PowerUpType.Shield:
                    return Color.cyan;

                case PowerUpType.CollectAllColors:
                    return Color.white;

                default:
                    return Color.white;
            }
        }

        #endregion
    }
}
