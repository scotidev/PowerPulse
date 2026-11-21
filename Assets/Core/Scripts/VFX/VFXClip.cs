using UnityEngine;

namespace PowerPulse.VFX
{
    /// <summary>
    /// Reusable visual effect clip. The <see cref="ParticleSystem"/> is created
    /// at runtime (and cached) so the prefab stays a lightweight data holder:
    /// every look is tuned in the Inspector, and the <c>CameraFX</c> only spawns,
    /// tints and recycles it through the shared object pool.
    /// </summary>
    public class VFXClip : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("Duration")]
        [SerializeField, Tooltip("How long the effect stays visible before the CameraFX recycles it (seconds).")]
        private float m_Duration = 0.6f;

        [Header("Time")]
        [SerializeField, Tooltip("If true, the particle system simulates on unscaled time (continues when Time.timeScale = 0). Used for death VFX that must persist on Game Over.")]
        private bool m_UseUnscaledTime = false;

        [Header("Emission")]
        [SerializeField, Tooltip("Number of particles emitted in the burst.")]
        private int m_BurstCount = 24;

        [SerializeField, Tooltip("Lifetime of each particle in seconds.")]
        private float m_StartLifetime = 0.5f;

        [Header("Motion")]
        [SerializeField, Tooltip("Initial speed of the particles in units per second.")]
        private float m_StartSpeed = 3f;

        [SerializeField, Tooltip("Size of each particle in world units.")]
        private float m_StartSize = 0.12f;

        [SerializeField, Tooltip("Gravity multiplier applied to the particles (0 = no gravity).")]
        private float m_Gravity = 0f;

        [Header("Shape")]
        [SerializeField, Tooltip("Shape of the emission volume.")]
        private ParticleSystemShapeType m_Shape = ParticleSystemShapeType.Sphere;

        [SerializeField, Tooltip("Radius of the emission volume in world units.")]
        private float m_ShapeRadius = 0.2f;

        [Header("Color")]
        [SerializeField, Tooltip("When checked, the color passed to Play() is used. When unchecked, the fixed color below is used.")]
        private bool m_UseEventColor = true;

        [SerializeField, Tooltip("Color used when 'Use Event Color' is unchecked.")]
        private Color m_FixedColor = Color.white;

        [SerializeField, Tooltip("Multiplier applied to the color so the particles exceed the Bloom threshold and glow.")]
        private float m_ColorEmission = 2f;

        #endregion


        #region STATIC

        /// <summary>Shared additive particle material, created once at runtime.</summary>
        private static Material s_Material;

        #endregion


        #region FIELDS

        private ParticleSystem m_Particles;
        private ParticleSystemRenderer m_Renderer;

        #endregion


        #region PROPERTIES

        /// <summary>How long the effect stays visible before being recycled.</summary>
        public float Duration => m_Duration;

        /// <summary>Whether this VFX uses unscaled time for simulation.</summary>
        public bool UseUnscaledTime => m_UseUnscaledTime;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            m_Particles = GetComponent<ParticleSystem>();

            if (m_Particles == null)
            {
                m_Particles = gameObject.AddComponent<ParticleSystem>();
            }

            m_Renderer = GetComponent<ParticleSystemRenderer>();

            if (m_Renderer == null)
            {
                m_Renderer = gameObject.AddComponent<ParticleSystemRenderer>();
            }

            Configure();
        }

        #endregion


        #region PUBLIC METHODS

        /// <summary>
        /// Plays the clip with the given color. Safe to call every time the clip
        /// is retrieved from the pool.
        /// </summary>
        /// <param name="color">Color used when 'Use Event Color' is checked.</param>
        public void Play(Color color)
        {
            if (m_Particles == null)
            {
                return;
            }

            ParticleSystem.MainModule main = m_Particles.main;
            main.startColor = m_UseEventColor ? color * m_ColorEmission : m_FixedColor * m_ColorEmission;

            m_Particles.Clear();
            m_Particles.Play();
        }

        /// <summary>
        /// Stops the effect and clears every particle, leaving the instance ready
        /// to be returned to the pool.
        /// </summary>
        public void Stop()
        {
            if (m_Particles != null)
            {
                m_Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>
        /// Applies the Inspector settings to the runtime particle system.
        /// </summary>
        private void Configure()
        {
            // A new ParticleSystem starts playing on Awake. Stop it before
            // changing the duration: Unity rejects that while it is playing.
            m_Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = m_Particles.main;
            main.duration = Mathf.Max(m_Duration, m_StartLifetime, 0.05f);
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = m_StartLifetime;
            main.startSpeed = m_StartSpeed;
            main.startSize = m_StartSize;
            main.gravityModifier = m_Gravity;
            main.maxParticles = Mathf.Max(m_BurstCount * 2, 64);
            main.startColor = m_FixedColor * m_ColorEmission;
            // CRITICAL: use unscaled time when configured so particles animate even when Time.timeScale = 0
            main.useUnscaledTime = m_UseUnscaledTime;

            ParticleSystem.EmissionModule emission = m_Particles.emission;
            emission.rateOverTime = 0f;
            short count = (short)Mathf.Max(1, m_BurstCount);
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count, count) });

            ParticleSystem.ShapeModule shape = m_Particles.shape;
            shape.shapeType = m_Shape;
            shape.radius = Mathf.Max(0.01f, m_ShapeRadius);

            m_Renderer.renderMode = ParticleSystemRenderMode.Billboard;
            m_Renderer.sharedMaterial = GetParticleMaterial();
            m_Renderer.sortingLayerID = 0;
        }

        /// <summary>
        /// Returns the shared additive particle material, creating it on first use.
        /// The URP particle shader is used when available, with a built-in fallback.
        /// </summary>
        /// <returns>The shared material, or <c>null</c> when no particle shader is found.</returns>
        private static Material GetParticleMaterial()
        {
            if (s_Material != null)
            {
                return s_Material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                Debug.LogWarning("[VFXClip] No particle shader found. The VFX will use the default material.", null);
                return null;
            }

            s_Material = new Material(shader) { name = "VFX_Particle (Runtime)" };
            s_Material.SetFloat("_Surface", 1f);
            s_Material.SetFloat("_SrcBlend", 1f);
            s_Material.SetFloat("_DstBlend", 1f);
            s_Material.SetFloat("_ZWrite", 0f);
            s_Material.SetColor("_BaseColor", Color.white);
            s_Material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            s_Material.renderQueue = 3000;

            return s_Material;
        }

        #endregion
    }
}
