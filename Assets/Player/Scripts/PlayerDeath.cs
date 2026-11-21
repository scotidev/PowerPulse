using System;
using System.Collections;
using UnityEngine;
using PowerPulse.Core;
using PowerPulse.Obstacles;
using PowerPulse.CameraSystem;

namespace PowerPulse.Player
{
    /// <summary>
    /// Owns the cinematic death sequence in 5 phases:
    /// 0. Killer Obstacle Explodes (immediate, unscaled time)
    /// 1. Sphere Expansion + Slow Motion (unscaled time)
    /// 2. Sphere Explodes -> spawns DeathVfx particles (via CameraFX.PlayDeath event)
    /// 3. Burst Phase: particles fly in slow motion (unscaled time)
    /// 4. Freeze Phase: timeScale lerps to 0 (unscaled time)
    /// Then closes the run (GameOver state).
    /// All timing and curves driven by Config_VFX asset.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerDeath : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("References")]
        [SerializeField, Tooltip("Score manager that closes the run and records the score.")]
        private ScoreManager m_ScoreManager;

        [SerializeField, Tooltip("Trail renderer disabled during death.")]
        private TrailRenderer m_Trail;

        [SerializeField, Tooltip("VFX settings asset for all death sequence parameters.")]
        private Config_VFX m_VfxConfig;

        [Header("Expansion Visual")]
        [SerializeField, Tooltip("Material used for the expanding sphere (emissive).")]
        private Material m_ExpansionMaterial;

        [Header("Killing Obstacle Reaction")]
        // Emission boost and scale now read from Config_VFX (KillerEmissionBoost, KillerExplosionScale)

        #endregion


        #region CONSTANTS

        /// <summary>Scale of the sphere at rest.</summary>
        private const float c_RestScale = 0.8f;

        #endregion


        #region FIELDS

        private Rigidbody m_Rb;
        private PlayerController m_Controller;
        private PlayerCollision m_Collision;
        private Renderer[] m_Renderers;
        private Material[] m_OriginalMaterials;
        private Color[] m_OriginalEmissionColors;
        private bool m_IsDying;

        // Killing obstacle state
        private Obstacle m_KillingObstacle;
        private Renderer m_KillerRenderer;
        private Material m_KillerMaterial;
        private Color m_KillerOriginalEmission;
        private Vector3 m_KillerOriginalScale;

        // Phase timing (cached from Config_VFX)
        private float m_KillerExplosionDelay;
        private float m_ExpansionDuration;
        private float m_BurstDuration;
        private float m_FreezeDuration;
        private float m_MaxExpansionScale;
        private AnimationCurve m_ExpansionCurve;
        private AnimationCurve m_SlowMotionCurve;

        // Progress reporting
        private event Action<float> OnDeathProgress;
        private bool m_GameOverTriggered;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_Rb = GetComponent<Rigidbody>();
            m_Controller = GetComponent<PlayerController>();
            m_Collision = GetComponent<PlayerCollision>();

            // Cache renderers and original materials
            m_Renderers = GetComponentsInChildren<Renderer>();
            m_OriginalMaterials = new Material[m_Renderers.Length];
            m_OriginalEmissionColors = new Color[m_Renderers.Length];

            for (int i = 0; i < m_Renderers.Length; i++)
            {
                m_OriginalMaterials[i] = m_Renderers[i].material;
                if (m_OriginalMaterials[i].HasProperty("_EmissionColor"))
                {
                    m_OriginalEmissionColors[i] = m_OriginalMaterials[i].GetColor("_EmissionColor");
                }
            }

            // Auto-resolve VFX config if not assigned
            if (m_VfxConfig == null)
            {
                m_VfxConfig = Resources.Load<Config_VFX>("VFXSettings");
            }

            CacheConfigValues();
        }

        private void OnEnable()
        {
            // Subscribe to death progress for external systems (UIManager, CameraFX)
            if (m_VfxConfig != null)
            {
                // External listeners can subscribe via PlayerDeath.OnDeathProgress
            }
        }

        private void OnDisable()
        {
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Starts the cinematic death sequence.
        /// </summary>
        /// <param name="killingObstacle">The obstacle that caused the death (may be null).</param>
        public void Die(Obstacle killingObstacle = null)
        {
            if (m_IsDying)
            {
                return;
            }

            m_IsDying = true;
            m_GameOverTriggered = false;
            m_KillingObstacle = killingObstacle;

            // Prepare killing obstacle reaction
            PrepareKillingObstacle();

            // Hide trail immediately
            if (m_Trail != null)
            {
                m_Trail.enabled = false;
                m_Trail.Clear();
            }

            // Disable player systems
            DisablePlayerSystems();

            // Start the cinematic sequence
            StartCoroutine(CinematicDeathRoutine());

            Debug.Log("[PlayerDeath] Cinematic death sequence started.");
        }

        /// <summary>
        /// Allows external systems (UIManager) to subscribe to death sequence progress (0-1).
        /// </summary>
        public event Action<float> DeathProgress
        {
            add => OnDeathProgress += value;
            remove => OnDeathProgress -= value;
        }

        /// <summary>
        /// Restores the player for a new run.
        /// </summary>
        public void ResetPlayer()
        {
            // CRITICAL: Restore time scale FIRST, before re-enabling anything
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            m_IsDying = false;
            m_GameOverTriggered = false;

            // Restore killing obstacle if it exists
            RestoreKillingObstacle();

            // Re-enable player systems
            EnablePlayerSystems();

            // Restore renderers and materials
            for (int i = 0; i < m_Renderers.Length; i++)
            {
                if (m_Renderers[i] != null)
                {
                    m_Renderers[i].enabled = true;
                    m_Renderers[i].material = m_OriginalMaterials[i];
                    if (m_OriginalMaterials[i].HasProperty("_EmissionColor"))
                    {
                        m_OriginalMaterials[i].SetColor("_EmissionColor", m_OriginalEmissionColors[i]);
                    }
                }
            }

            // Restore physics
            if (m_Rb != null)
            {
                m_Rb.isKinematic = false;
                m_Rb.linearVelocity = Vector3.zero;
                m_Rb.angularVelocity = Vector3.zero;
            }

            // Restore trail
            if (m_Trail != null)
            {
                m_Trail.enabled = true;
                m_Trail.Clear();
            }

            if (m_Controller != null)
            {
                m_Controller.ResetMovement();
            }

            transform.localScale = Vector3.one * c_RestScale;
            gameObject.SetActive(true);

            Debug.Log("[PlayerDeath] Player restored for a new run.");
        }

        #endregion


        #region PRIVATE METHODS

        private void CacheConfigValues()
        {
            if (m_VfxConfig == null) return;

            m_KillerExplosionDelay = m_VfxConfig.KillerExplosionDelay;
            m_ExpansionDuration = m_VfxConfig.ExpansionDuration;
            m_BurstDuration = m_VfxConfig.BurstDuration;
            m_FreezeDuration = m_VfxConfig.FreezeDuration;
            m_MaxExpansionScale = m_VfxConfig.MaxExpansionScale;
            m_ExpansionCurve = m_VfxConfig.ExpansionCurve;
            m_SlowMotionCurve = m_VfxConfig.SlowMotionCurve;
        }

        private void DisablePlayerSystems()
        {
            if (m_Controller != null) m_Controller.enabled = false;
            if (m_Collision != null) m_Collision.enabled = false;

            if (m_Rb != null)
            {
                m_Rb.isKinematic = true;
                m_Rb.linearVelocity = Vector3.zero;
                m_Rb.angularVelocity = Vector3.zero;
            }
        }

        private void EnablePlayerSystems()
        {
            if (m_Controller != null) m_Controller.enabled = true;
            if (m_Collision != null) m_Collision.enabled = true;
        }

        private void PrepareKillingObstacle()
        {
            if (m_KillingObstacle == null) return;

            m_KillerRenderer = m_KillingObstacle.GetComponentInChildren<Renderer>();
            if (m_KillerRenderer == null) return;

            m_KillerMaterial = m_KillerRenderer.material;
            m_KillerOriginalScale = m_KillingObstacle.transform.localScale;

            if (m_KillerMaterial.HasProperty("_EmissionColor"))
            {
                m_KillerOriginalEmission = m_KillerMaterial.GetColor("_EmissionColor");
                // Boost emission dramatically
                m_KillerMaterial.SetColor("_EmissionColor", m_KillerOriginalEmission * m_VfxConfig.KillerEmissionBoost);
            }

            // Scale up the killing obstacle slightly for its explosion
            m_KillingObstacle.transform.localScale = m_KillerOriginalScale * m_VfxConfig.KillerExplosionScale;

            // Make it kinematic so it moves with the track but doesn't physics-interact
            Rigidbody killerRb = m_KillingObstacle.GetComponent<Rigidbody>();
            if (killerRb != null) killerRb.isKinematic = true;
        }

        private void RestoreKillingObstacle()
        {
            if (m_KillingObstacle == null || m_KillerRenderer == null) return;

            if (m_KillerMaterial != null && m_KillerMaterial.HasProperty("_EmissionColor"))
            {
                m_KillerMaterial.SetColor("_EmissionColor", m_KillerOriginalEmission);
            }

            m_KillingObstacle.transform.localScale = m_KillerOriginalScale;

            Rigidbody killerRb = m_KillingObstacle.GetComponent<Rigidbody>();
            if (killerRb != null) killerRb.isKinematic = false;
        }

        /// <summary>
        /// Cinematic death sequence in 5 phases:
        /// 0. Killer Explosion (immediate)
        /// 1. Sphere Expansion + Slow Motion
        /// 2. Sphere Explosion (trigger CameraFX.PlayDeath)
        /// 3. Burst Phase (particles fly in slow motion)
        /// 4. Freeze Phase (timeScale lerps to 0)
        /// </summary>
        private IEnumerator CinematicDeathRoutine()
        {
            // ===== PHASE 0: KILLER OBSTACLE EXPLODES =====
            yield return StartCoroutine(KillerExplosionPhase());

            // ===== PHASE 1: SPHERE EXPANSION + SLOW MOTION =====
            yield return StartCoroutine(ExpansionPhase());

            // ===== PHASE 2: SPHERE EXPLODES =====
            TriggerSphereExplosion();

            // ===== PHASE 3: BURST PHASE (particles fly in slow motion) =====
            yield return StartCoroutine(BurstPhase());

            // ===== PHASE 4: FREEZE PHASE (timeScale lerps to 0) =====
            yield return StartCoroutine(FreezePhase());

            // End: close run (triggers GameOver state)
            if (m_ScoreManager != null)
            {
                m_ScoreManager.CloseRun();
            }

            Debug.Log("[PlayerDeath] Cinematic death finished. Run closed.");
        }

        private IEnumerator KillerExplosionPhase()
        {
            // Spawn obstacle explosion VFX at obstacle position via CameraFX
            if (m_KillingObstacle != null && m_VfxConfig != null && m_VfxConfig.DeathObstacleExplosionVfx != null)
            {
                CameraFX cameraFX = FindFirstObjectByType<CameraFX>();
                if (cameraFX != null)
                {
                    Color killerColor = GetKillerColor();
                    cameraFX.PlayVfx(
                        m_VfxConfig.DeathObstacleExplosionVfx,
                        m_KillingObstacle.transform.position,
                        killerColor,
                        m_VfxConfig.DeathPrewarm,
                        m_VfxConfig.KillerExplosionScale,
                        false // obstacle explosion uses scaled time (happens before slow motion)
                    );
                }
            }

            // Wait tiny delay for explosion to be visible
            yield return new WaitForSecondsRealtime(m_KillerExplosionDelay);

            // Hide killer obstacle (it "exploded")
            HideKillerObstacle();
        }

        private void HideKillerObstacle()
        {
            if (m_KillerRenderer != null)
            {
                m_KillerRenderer.enabled = false;
            }
        }

        private IEnumerator ExpansionPhase()
        {
            float elapsed = 0f;
            float duration = m_ExpansionDuration;

            // Swap to expansion material if provided
            if (m_ExpansionMaterial != null)
            {
                for (int i = 0; i < m_Renderers.Length; i++)
                {
                    if (m_Renderers[i] != null)
                    {
                        m_Renderers[i].material = m_ExpansionMaterial;
                    }
                }
            }

            Vector3 startScale = transform.localScale;
            Vector3 targetScale = Vector3.one * m_MaxExpansionScale * c_RestScale;

            // Phase 1 weight in total sequence (expansion is roughly 30% of total)
            const float phaseWeight = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Sphere expansion via curve
                float expansionT = m_ExpansionCurve.Evaluate(t);
                transform.localScale = Vector3.Lerp(startScale, targetScale, expansionT);

                // Global slow motion: timeScale follows SlowMotionCurve based on total sequence progress
                float sequenceProgress = Mathf.Lerp(0f, phaseWeight, t);
                float timeScale = m_SlowMotionCurve.Evaluate(sequenceProgress);
                Time.timeScale = timeScale;
                Time.fixedDeltaTime = 0.02f * timeScale;

                // Report progress for Game Over trigger
                ReportDeathProgress(sequenceProgress);

                yield return null;
            }

            // Ensure exact end state
            transform.localScale = targetScale;
            ReportDeathProgress(phaseWeight);
        }

        private void TriggerSphereExplosion()
        {
            // Hide the sphere mesh
            for (int i = 0; i < m_Renderers.Length; i++)
            {
                if (m_Renderers[i] != null)
                {
                    m_Renderers[i].enabled = false;
                }
            }

            // Trigger CameraFX.PlayDeath to spawn the main DeathVfx particles
            // These particles will use unscaled time and persist on Game Over if configured
            CameraFX cameraFX = FindFirstObjectByType<CameraFX>();
            if (cameraFX != null && m_ScoreManager != null)
            {
                Color deathColor = cameraFX.GetPaletteColor(m_ScoreManager.ActiveColor);
                Vector3 deathPosition = transform.position;
                cameraFX.PlayDeath(deathPosition, deathColor);
            }
        }

        private IEnumerator BurstPhase()
        {
            float elapsed = 0f;
            float duration = m_BurstDuration;

            // Phase 2 weight (burst is roughly 40% of total, starting at 0.3)
            const float phaseStartProgress = 0.3f;
            const float phaseWeight = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Continue slow motion curve through total sequence
                float sequenceProgress = Mathf.Lerp(phaseStartProgress, phaseStartProgress + phaseWeight, t);
                float timeScale = m_SlowMotionCurve.Evaluate(sequenceProgress);
                Time.timeScale = timeScale;
                Time.fixedDeltaTime = 0.02f * timeScale;

                ReportDeathProgress(sequenceProgress);
                yield return null;
            }

            ReportDeathProgress(phaseStartProgress + phaseWeight);
        }

        private IEnumerator FreezePhase()
        {
            float elapsed = 0f;
            float duration = m_FreezeDuration;

            float startTimeScale = Time.timeScale;
            const float phaseStartProgress = 0.7f; // after expansion (0.3) + burst (0.4)

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Lerp from current timeScale to 0
                float timeScale = Mathf.Lerp(startTimeScale, 0f, t);
                Time.timeScale = timeScale;
                Time.fixedDeltaTime = 0.02f * timeScale;

                float sequenceProgress = Mathf.Lerp(phaseStartProgress, 1f, t);
                ReportDeathProgress(sequenceProgress);

                yield return null;
            }

            // Fully pause
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
            ReportDeathProgress(1f);
        }

        private void ReportDeathProgress(float progress)
        {
            OnDeathProgress?.Invoke(progress);

            // Trigger Game Over panel at configured progress
            if (!m_GameOverTriggered && m_VfxConfig != null && progress >= m_VfxConfig.GameOverTriggerProgress)
            {
                m_GameOverTriggered = true;
                // The UIManager listens to this and shows Game Over panel
                Debug.Log($"[PlayerDeath] Game Over trigger at progress {progress:0.00}");
            }
        }

        private Color GetKillerColor()
        {
            if (m_KillingObstacle != null)
            {
                return m_KillingObstacle.GetComponent<Obstacle>()?.ActiveColor switch
                {
                    ColorId.Red => Color.red,
                    ColorId.Pink => Color.magenta,
                    ColorId.Blue => Color.blue,
                    ColorId.Cyan => Color.cyan,
                    ColorId.Purple => new Color(0.5f, 0f, 1f),
                    _ => Color.white
                };
            }
            return Color.white;
        }

        #endregion
    }
}
