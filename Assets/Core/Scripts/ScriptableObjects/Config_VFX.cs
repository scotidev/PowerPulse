using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Tuning asset for the camera effects and the visual effects (VFX). Every
    /// intensity, duration and prefab used by the <c>CameraFX</c> lives here so
    /// the juice can be adjusted without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/VFX Settings")]
    public class Config_VFX : ScriptableObject
    {
        #region SERIALIZED FIELDS

        [Header("Shake")]
        [SerializeField, Tooltip("Camera shake amplitude when an obstacle is collected (world units).")]
        private float m_CollectShakeAmplitude = 0.08f;

        [SerializeField, Tooltip("Camera shake duration when an obstacle is collected (seconds).")]
        private float m_CollectShakeDuration = 0.18f;

        [SerializeField, Tooltip("Camera shake frequency when an obstacle is collected.")]
        private float m_CollectShakeFrequency = 25f;

        [SerializeField, Tooltip("Camera shake amplitude when the player dies (world units).")]
        private float m_DeathShakeAmplitude = 0.35f;

        [SerializeField, Tooltip("Camera shake duration when the player dies (seconds).")]
        private float m_DeathShakeDuration = 0.6f;

        [SerializeField, Tooltip("Camera shake frequency when the player dies.")]
        private float m_DeathShakeFrequency = 18f;

        [Header("FOV")]
        [SerializeField, Tooltip("Extra field of view applied while a boost is active (degrees).")]
        private float m_BoostFovPunch = 8f;

        [SerializeField, Tooltip("How fast the field of view follows the boost target (higher = tighter).")]
        private float m_FovSpeed = 8f;

        [SerializeField, Tooltip("Time before boost ends to start returning FOV to normal (seconds). Gives player warning that boost is expiring.")]
        private float m_BoostFovReturnTime = 1.5f;

        [SerializeField, Tooltip("Time before boost ends to start returning track speed to normal (seconds). Gives player warning that boost is expiring.")]
        private float m_BoostSpeedReturnTime = 1.5f;

        [SerializeField, Tooltip("Speed multiplier applied to track speed while boost is active (1 = no change).")]
        private float m_BoostSpeedMultiplier = 1.25f;

        [SerializeField, Tooltip("Curve for FOV return transition (X = progress 0-1, Y = multiplier 0-1).")]
        private AnimationCurve m_BoostReturnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Flash")]
        [SerializeField, Tooltip("Color of the full-screen flash.")]
        private Color m_FlashColor = Color.white;

        [SerializeField, Tooltip("Maximum alpha of the full-screen flash (0 to 1).")]
        private float m_FlashAlpha = 0.55f;

        [SerializeField, Tooltip("Flash duration on death (seconds).")]
        private float m_DeathFlashDuration = 0.35f;

        [SerializeField, Tooltip("Flash duration when crossing a portal (seconds).")]
        private float m_PortalFlashDuration = 0.22f;

        [Header("Post-processing")]
        [SerializeField, Tooltip("Extra Bloom intensity while a boost is active.")]
        private float m_BoostBloomIntensity = 1.5f;

        [SerializeField, Tooltip("Extra Bloom intensity while the death effect plays.")]
        private float m_DeathBloomIntensity = 0.8f;

        [SerializeField, Tooltip("Extra Chromatic Aberration intensity while the death effect plays.")]
        private float m_DeathChromaticAberration = 0.6f;

        [SerializeField, Tooltip("How long the death post-processing boost lasts (seconds).")]
        private float m_DeathPostFxDuration = 0.5f;

        [SerializeField, Tooltip("How fast Bloom and Chromatic Aberration return to their base values (higher = faster).")]
        private float m_PostProcessingRestoreSpeed = 6f;

        [Header("VFX Prefabs")]
        [SerializeField, Tooltip("VFX spawned when an obstacle is collected.")]
        private GameObject m_CollectVfx;

        [SerializeField, Tooltip("Single VFX for death: sphere explosion particles + screen distortion combined.")]
        private GameObject m_DeathVfx;

        [SerializeField, Tooltip("Separate VFX for the killing obstacle explosion (plays BEFORE sphere expands).")]
        private GameObject m_DeathObstacleExplosionVfx;

        [SerializeField, Tooltip("VFX spawned when the player dashes.")]
        private GameObject m_DashVfx;

        [SerializeField, Tooltip("VFX spawned when the player crosses a portal.")]
        private GameObject m_PortalVfx;

        [SerializeField, Tooltip("VFX for Shield power-up: white explosive burst.")]
        private GameObject m_ShieldVfx;

        [SerializeField, Tooltip("VFX for CollectAllColors power-up: multi-color explosive burst.")]
        private GameObject m_CollectAllColorsVfx;

        [SerializeField, Tooltip("Fallback VFX for power-ups whose catalog entry has no VfxPrefab assigned.")]
        private GameObject m_PowerUpVfx;

        [Header("Death Sequence")]
        [SerializeField, Tooltip("Delay before killing obstacle explodes (seconds, unscaled time).")]
        private float m_KillerExplosionDelay = 0.05f;

        [SerializeField, Tooltip("Scale multiplier for killing obstacle explosion VFX.")]
        private float m_KillerExplosionScale = 2f;

        [SerializeField, Tooltip("Emission boost for killing obstacle before it explodes.")]
        private float m_KillerEmissionBoost = 15f;

        [SerializeField, Tooltip("Maximum scale of the sphere during expansion (rest scale = 1).")]
        private float m_MaxExpansionScale = 4f;

        [SerializeField, Tooltip("Duration of sphere expansion phase (seconds, unscaled time).")]
        private float m_ExpansionDuration = 0.8f;

        [SerializeField, Tooltip("Curve for sphere expansion (X = progress 0-1, Y = scale multiplier 0-1).")]
        private AnimationCurve m_ExpansionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField, Tooltip("Duration of particle burst phase after sphere explodes (seconds, unscaled time).")]
        private float m_BurstDuration = 1.2f;

        [SerializeField, Tooltip("Duration of final freeze lerp to timeScale 0 (seconds, unscaled time).")]
        private float m_FreezeDuration = 0.6f;

        [SerializeField, Tooltip("Slow motion curve: X = total sequence progress (0-1), Y = timeScale (1-0).")]
        private AnimationCurve m_SlowMotionCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [SerializeField, Tooltip("At what sequence progress (0-1) to trigger Game Over panel.")]
        [Range(0f, 1f)]
        private float m_GameOverTriggerProgress = 0.8f;

        [SerializeField, Tooltip("If true, death VFX particles persist frozen at timeScale=0 until next run.")]
        private bool m_DeathVfxPersistOnGameOver = true;

        [Header("Pooling")]
        [SerializeField, Tooltip("How many collect VFX instances are pre-created.")]
        private int m_CollectPrewarm = 8;

        [SerializeField, Tooltip("How many death VFX instances are pre-created.")]
        private int m_DeathPrewarm = 4;

        [SerializeField, Tooltip("How many dash VFX instances are pre-created.")]
        private int m_DashPrewarm = 6;

        [SerializeField, Tooltip("How many portal VFX instances are pre-created.")]
        private int m_PortalPrewarm = 2;

        [SerializeField, Tooltip("How many Shield VFX instances are pre-created.")]
        private int m_ShieldPrewarm = 4;

        [SerializeField, Tooltip("How many CollectAllColors VFX instances are pre-created.")]
        private int m_CollectAllColorsPrewarm = 4;

        [SerializeField, Tooltip("How many power-up VFX instances are pre-created.")]
        private int m_PowerUpPrewarm = 4;

        [SerializeField, Tooltip("Uniform scale applied to every spawned VFX.")]
        private float m_VfxScale = 1f;

        [SerializeField, Tooltip("Fallback duration (seconds) for a VFX prefab without a VFXClip component.")]
        private float m_DefaultVfxDuration = 0.6f;

        #endregion


        #region PROPERTIES

        /// <summary>Camera shake amplitude when an obstacle is collected.</summary>
        public float CollectShakeAmplitude => m_CollectShakeAmplitude;

        /// <summary>Camera shake duration when an obstacle is collected.</summary>
        public float CollectShakeDuration => m_CollectShakeDuration;

        /// <summary>Camera shake frequency when an obstacle is collected.</summary>
        public float CollectShakeFrequency => m_CollectShakeFrequency;

        /// <summary>Camera shake amplitude when the player dies.</summary>
        public float DeathShakeAmplitude => m_DeathShakeAmplitude;

        /// <summary>Camera shake duration when the player dies.</summary>
        public float DeathShakeDuration => m_DeathShakeDuration;

        /// <summary>Camera shake frequency when the player dies.</summary>
        public float DeathShakeFrequency => m_DeathShakeFrequency;

        /// <summary>Extra field of view applied while a boost is active.</summary>
        public float BoostFovPunch => m_BoostFovPunch;

        /// <summary>How fast the field of view follows the boost target.</summary>
        public float FovSpeed => m_FovSpeed;

        /// <summary>Time before boost ends to start returning FOV to normal (seconds).</summary>
        public float BoostFovReturnTime => m_BoostFovReturnTime;

        /// <summary>Time before boost ends to start returning track speed to normal (seconds).</summary>
        public float BoostSpeedReturnTime => m_BoostSpeedReturnTime;

        /// <summary>Speed multiplier applied to track speed while boost is active.</summary>
        public float BoostSpeedMultiplier => m_BoostSpeedMultiplier;

        /// <summary>Curve for FOV/speed return transition.</summary>
        public AnimationCurve BoostReturnCurve => m_BoostReturnCurve;

        /// <summary>Color of the full-screen flash.</summary>
        public Color FlashColor => m_FlashColor;

        /// <summary>Maximum alpha of the full-screen flash.</summary>
        public float FlashAlpha => m_FlashAlpha;

        /// <summary>Flash duration on death.</summary>
        public float DeathFlashDuration => m_DeathFlashDuration;

        /// <summary>Flash duration when crossing a portal.</summary>
        public float PortalFlashDuration => m_PortalFlashDuration;

        /// <summary>Extra Bloom intensity while a boost is active.</summary>
        public float BoostBloomIntensity => m_BoostBloomIntensity;

        /// <summary>Extra Bloom intensity while the death effect plays.</summary>
        public float DeathBloomIntensity => m_DeathBloomIntensity;

        /// <summary>Extra Chromatic Aberration intensity while the death effect plays.</summary>
        public float DeathChromaticAberration => m_DeathChromaticAberration;

        /// <summary>How long the death post-processing boost lasts.</summary>
        public float DeathPostFxDuration => m_DeathPostFxDuration;

        /// <summary>How fast Bloom and Chromatic Aberration return to their base values.</summary>
        public float PostProcessingRestoreSpeed => m_PostProcessingRestoreSpeed;

        /// <summary>VFX spawned when an obstacle is collected.</summary>
        public GameObject CollectVfx => m_CollectVfx;

        /// <summary>Single VFX for death: sphere explosion particles + screen distortion combined.</summary>
        public GameObject DeathVfx => m_DeathVfx;

        /// <summary>Separate VFX for the killing obstacle explosion (plays BEFORE sphere expands).</summary>
        public GameObject DeathObstacleExplosionVfx => m_DeathObstacleExplosionVfx;

        /// <summary>VFX spawned when the player dashes.</summary>
        public GameObject DashVfx => m_DashVfx;

        /// <summary>VFX spawned when the player crosses a portal.</summary>
        public GameObject PortalVfx => m_PortalVfx;

        /// <summary>VFX for Shield power-up: white explosive burst.</summary>
        public GameObject ShieldVfx => m_ShieldVfx;

        /// <summary>VFX for CollectAllColors power-up: multi-color explosive burst.</summary>
        public GameObject CollectAllColorsVfx => m_CollectAllColorsVfx;

        /// <summary>Fallback VFX for power-ups.</summary>
        public GameObject PowerUpVfx => m_PowerUpVfx;

        /// <summary>Delay before killing obstacle explodes (unscaled time).</summary>
        public float KillerExplosionDelay => m_KillerExplosionDelay;

        /// <summary>Scale multiplier for killing obstacle explosion VFX.</summary>
        public float KillerExplosionScale => m_KillerExplosionScale;

        /// <summary>Emission boost for killing obstacle before it explodes.</summary>
        public float KillerEmissionBoost => m_KillerEmissionBoost;

        /// <summary>Maximum scale of the sphere during expansion.</summary>
        public float MaxExpansionScale => m_MaxExpansionScale;

        /// <summary>Duration of sphere expansion phase (unscaled time).</summary>
        public float ExpansionDuration => m_ExpansionDuration;

        /// <summary>Curve for sphere expansion (X = progress 0-1, Y = scale multiplier 0-1).</summary>
        public AnimationCurve ExpansionCurve => m_ExpansionCurve;

        /// <summary>Duration of particle burst phase after sphere explodes (unscaled time).</summary>
        public float BurstDuration => m_BurstDuration;

        /// <summary>Duration of final freeze lerp to timeScale 0 (unscaled time).</summary>
        public float FreezeDuration => m_FreezeDuration;

        /// <summary>Slow motion curve: X = total sequence progress (0-1), Y = timeScale (1-0).</summary>
        public AnimationCurve SlowMotionCurve => m_SlowMotionCurve;

        /// <summary>At what sequence progress (0-1) to trigger Game Over panel.</summary>
        public float GameOverTriggerProgress => m_GameOverTriggerProgress;

        /// <summary>If true, death VFX particles persist frozen at timeScale=0 until next run.</summary>
        public bool DeathVfxPersistOnGameOver => m_DeathVfxPersistOnGameOver;

        /// <summary>How many death VFX instances to prewarm.</summary>
        public int DeathPrewarm => m_DeathPrewarm;

        /// <summary>How many collect VFX instances are pre-created.</summary>
        public int CollectPrewarm => m_CollectPrewarm;

        /// <summary>How many dash VFX instances are pre-created.</summary>
        public int DashPrewarm => m_DashPrewarm;

        /// <summary>How many portal VFX instances are pre-created.</summary>
        public int PortalPrewarm => m_PortalPrewarm;

        /// <summary>How many Shield VFX instances are pre-created.</summary>
        public int ShieldPrewarm => m_ShieldPrewarm;

        /// <summary>How many CollectAllColors VFX instances are pre-created.</summary>
        public int CollectAllColorsPrewarm => m_CollectAllColorsPrewarm;

        /// <summary>How many power-up VFX instances are pre-created.</summary>
        public int PowerUpPrewarm => m_PowerUpPrewarm;

        /// <summary>Uniform scale applied to every spawned VFX.</summary>
        public float VfxScale => m_VfxScale;

        /// <summary>Fallback duration for a VFX prefab without a VFXClip component.</summary>
        public float DefaultVfxDuration => m_DefaultVfxDuration;

        #endregion
    }
}
