using System;
using System.Collections;
using UnityEngine;
using PowerPulse.Core;
using PowerPulse.Obstacles;

namespace PowerPulse.Player
{
    /// <summary>
    /// Owns the cinematic death sequence: slow-motion sphere expansion,
    /// explosive particle burst, then full freeze. The killing obstacle
    /// is highlighted so it doesn't clip inside the expanding sphere.
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

        [SerializeField, Tooltip("VFX settings asset for slow-motion curve.")]
        private Config_VFX m_VfxConfig;

        [Header("Expansion Visual")]
        [SerializeField, Tooltip("Material used for the expanding sphere (emissive).")]
        private Material m_ExpansionMaterial;

        [SerializeField, Tooltip("Maximum scale multiplier during expansion (default 4x).")]
        private float m_MaxExpansionScale = 4f;

        [SerializeField, Tooltip("Duration of the expansion phase (seconds, unscaled time).")]
        private float m_ExpansionDuration = 1.5f;

        [Header("Burst (Explosion)")]
        [SerializeField, Tooltip("Duration of the particle burst phase (seconds, unscaled time).")]
        private float m_BurstDuration = 1f;

        [SerializeField, Tooltip("Duration of the final freeze to timeScale 0 (seconds, unscaled time).")]
        private float m_FreezeDuration = 0.5f;

        [Header("Killing Obstacle Reaction")]
        [SerializeField, Tooltip("How much to boost the killing obstacle's emission.")]
        private float m_KillerEmissionBoost = 10f;

        [SerializeField, Tooltip("Scale multiplier for the killing obstacle during expansion.")]
        private float m_KillerScaleBoost = 1.5f;

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
        }

        private void Update()
        {
            // Death sequence runs via coroutine
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
        /// Restores the player for a new run.
        /// </summary>
        public void ResetPlayer()
        {
            // CRITICAL: Restore time scale FIRST, before re-enabling anything
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            m_IsDying = false;

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
                m_KillerMaterial.SetColor("_EmissionColor", m_KillerOriginalEmission * m_KillerEmissionBoost);
            }

            // Scale up the killing obstacle slightly so it wraps around the expanding sphere
            m_KillingObstacle.transform.localScale = m_KillerOriginalScale * m_KillerScaleBoost;

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
        /// Cinematic death sequence in 3 phases:
        /// 1. Expansion (sphere grows, killer obstacle wraps, slow motion)
        /// 2. Burst (particles explode AFTER expansion, continue slow motion)
        /// 3. Freeze (time scale lerps to 0)
        /// </summary>
        private IEnumerator CinematicDeathRoutine()
        {
            // --- Phase 1: EXPANSION (no particles yet) ---
            yield return StartCoroutine(ExpansionPhase());

            // --- Phase 2: BURST (particles spawn AFTER expansion) ---
            TriggerExplosionBurst();
            yield return StartCoroutine(BurstPhase());

            // --- Phase 3: FREEZE (time scale lerps to 0) ---
            yield return StartCoroutine(FreezePhase());

            // End: close run
            if (m_ScoreManager != null)
            {
                m_ScoreManager.CloseRun();
            }

            Debug.Log("[PlayerDeath] Cinematic death finished. Run closed.");
        }

        private IEnumerator ExpansionPhase()
        {
            float elapsed = 0f;
            float duration = m_ExpansionDuration;
            AnimationCurve curve = GetSlowMotionCurve();

            // Switch to expansion material if provided
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
            Vector3 targetScale = Vector3.one * m_MaxExpansionScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Sphere expansion (ease out for dramatic feel)
                float expansionT = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
                transform.localScale = Vector3.Lerp(startScale, targetScale, expansionT);

                // Killer obstacle scales with sphere (wraps around)
                if (m_KillingObstacle != null)
                {
                    m_KillingObstacle.transform.localScale = Vector3.Lerp(
                        m_KillerOriginalScale * m_KillerScaleBoost,
                        m_KillerOriginalScale * m_MaxExpansionScale * 0.8f,
                        expansionT);
                }

                // Global slow motion: timeScale follows curve
                float timeScale = curve.Evaluate(t);
                Time.timeScale = timeScale;
                Time.fixedDeltaTime = 0.02f * timeScale;

                yield return null;
            }

            // Ensure exact end state
            transform.localScale = targetScale;
            Time.timeScale = curve.Evaluate(1f);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        private void TriggerExplosionBurst()
        {
            // Hide the sphere mesh
            for (int i = 0; i < m_Renderers.Length; i++)
            {
                if (m_Renderers[i] != null)
                {
                    m_Renderers[i].enabled = false;
                }
            }

            // Hide killer obstacle
            if (m_KillerRenderer != null)
            {
                m_KillerRenderer.enabled = false;
            }

            // The CameraFX.PlayDeath() will be called via OnPlayerDied event
            // which spawns the DeathExplosionVfx particles at our position.
            // Those particles will also be in slow motion because timeScale < 1.
        }

        private IEnumerator BurstPhase()
        {
            float elapsed = 0f;
            float duration = m_BurstDuration;
            AnimationCurve curve = GetSlowMotionCurve();

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Continue slow motion curve (progress through total sequence)
                float totalProgress = Mathf.Clamp01(
                    (m_ExpansionDuration + elapsed) / (m_ExpansionDuration + m_BurstDuration + m_FreezeDuration));
                float timeScale = curve.Evaluate(totalProgress);
                Time.timeScale = timeScale;
                Time.fixedDeltaTime = 0.02f * timeScale;

                yield return null;
            }
        }

        private IEnumerator FreezePhase()
        {
            float elapsed = 0f;
            float duration = m_FreezeDuration;

            float startTimeScale = Time.timeScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Lerp from current timeScale to 0
                float timeScale = Mathf.Lerp(startTimeScale, 0f, t);
                Time.timeScale = timeScale;
                Time.fixedDeltaTime = 0.02f * timeScale;

                yield return null;
            }

            // Fully pause
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
        }

        private AnimationCurve GetSlowMotionCurve()
        {
            if (m_VfxConfig != null && m_VfxConfig.SlowMotionCurve != null && m_VfxConfig.SlowMotionCurve.length > 0)
            {
                return m_VfxConfig.SlowMotionCurve;
            }
            return AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        }

        #endregion
    }
}
