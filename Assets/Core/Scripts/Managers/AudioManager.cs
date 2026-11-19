using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace PowerPulse.Core
{
    /// <summary>
    /// Persistent audio service. Hosts the looping music AudioSource, a pool of
    /// AudioSources for one-shot SFX and routes everything through an
    /// <see cref="AudioMixer"/> with exposed volume parameters. Created by the
    /// Bootstraper on a persistent GameObject.
    /// </summary>
    public class AudioManager : MonoBehaviour, IAudioService
    {
        #region STATIC

        /// <summary>Mixer parameter that silences the output.</summary>
        private const float c_MutedDecibels = -80f;

        /// <summary>Mixer parameter for a linear volume of 1.</summary>
        private const float c_MaxDecibels = 0f;

        /// <summary>Floor used before the log conversion, to avoid log(0).</summary>
        private const float c_VolumeFloor = 0.0001f;

        /// <summary>Number of pooled SFX sources.</summary>
        private const int c_SfxPoolSize = 6;

        #endregion


        #region SERIALIZED FIELDS

        [Header("Mixer")]
        [SerializeField, Tooltip("Audio mixer that owns the Master/Music/Sfx volume parameters.")]
        private AudioMixer m_Mixer;

        [Header("Exposed Parameters")]
        [SerializeField, Tooltip("Name of the exposed master volume parameter.")]
        private string m_MasterParameter = "MasterVolume";

        [SerializeField, Tooltip("Name of the exposed music volume parameter.")]
        private string m_MusicParameter = "MusicVolume";

        [SerializeField, Tooltip("Name of the exposed SFX volume parameter.")]
        private string m_SfxParameter = "SfxVolume";

        [Header("Music")]
        [SerializeField, Tooltip("AudioSource used for the looping music. Created in runtime when empty.")]
        private AudioSource m_MusicSource;

        [Header("SFX Pool")]
        [SerializeField, Tooltip("Number of AudioSources pooled for one-shot SFX.")]
        private int m_SfxPoolSize = c_SfxPoolSize;

        #endregion


        #region FIELDS

        private readonly List<AudioSource> m_SfxPool = new List<AudioSource>();
        private float m_LastMasterDecibels = c_MaxDecibels;
        private bool m_MixerUsable = true;

        #endregion


        #region UNITY CALLBACKS

        private void Awake()
        {
            BuildSfxPool();

            if (m_MusicSource == null)
            {
                m_MusicSource = gameObject.AddComponent<AudioSource>();
            }

            m_MusicSource.loop = true;
            m_MusicSource.playOnAwake = false;

            ValidateMixer();
        }

        #endregion


        #region PUBLIC METHODS

        /// <inheritdoc />
        public void PlayMusic(AudioClip clip)
        {
            if (m_MusicSource == null || clip == null)
            {
                return;
            }

            m_MusicSource.clip = clip;
            m_MusicSource.Play();
        }

        /// <inheritdoc />
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource source = GetFreeSfxSource();
            if (source == null)
            {
                Debug.LogWarning("[AudioManager] No free AudioSource in the SFX pool.");
                return;
            }

            source.clip = clip;
            source.Play();
        }

        /// <inheritdoc />
        public void SetMasterVolume(float volume)
        {
            SetDecibels(m_MasterParameter, ToDecibels(volume));
        }

        /// <inheritdoc />
        public void SetMusicVolume(float volume)
        {
            SetDecibels(m_MusicParameter, ToDecibels(volume));
        }

        /// <inheritdoc />
        public void SetSfxVolume(float volume)
        {
            SetDecibels(m_SfxParameter, ToDecibels(volume));
        }

        /// <inheritdoc />
        public void SetMuted(bool muted)
        {
            if (m_Mixer == null || !m_MixerUsable)
            {
                return;
            }

            m_Mixer.SetFloat(m_MasterParameter, muted ? c_MutedDecibels : m_LastMasterDecibels);
        }

        #endregion


        #region PRIVATE METHODS

        /// <summary>Creates the pooled SFX AudioSources as children of this GameObject.</summary>
        private void BuildSfxPool()
        {
            m_SfxPool.Clear();

            for (int i = 0; i < m_SfxPoolSize; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                m_SfxPool.Add(source);
            }
        }

        /// <summary>Returns the first idle SFX source, or null when all are busy.</summary>
        private AudioSource GetFreeSfxSource()
        {
            for (int i = 0; i < m_SfxPool.Count; i++)
            {
                if (!m_SfxPool[i].isPlaying)
                {
                    return m_SfxPool[i];
                }
            }

            return null;
        }

        /// <summary>Converts a normalized volume (0-1) into decibels.</summary>
        private float ToDecibels(float normalized)
        {
            float clamped = Mathf.Clamp01(normalized);
            return Mathf.Log10(Mathf.Max(clamped, c_VolumeFloor)) * 20f;
        }

        /// <summary>Applies a decibel value to an exposed mixer parameter.</summary>
        private void SetDecibels(string parameter, float decibels)
        {
            if (m_Mixer == null || !m_MixerUsable)
            {
                return;
            }

            if (parameter == m_MasterParameter)
            {
                m_LastMasterDecibels = decibels;
            }

            m_Mixer.SetFloat(parameter, decibels);
        }

        /// <summary>Logs a warning when the mixer or its parameters are missing.</summary>
        private void ValidateMixer()
        {
            if (m_Mixer == null)
            {
                Debug.LogWarning("[AudioManager] No AudioMixer assigned. Audio volume will not change.");
                m_MixerUsable = false;
                return;
            }

            bool hasMaster = HasParameter(m_MasterParameter);
            bool hasMusic = HasParameter(m_MusicParameter);
            bool hasSfx = HasParameter(m_SfxParameter);

            m_MixerUsable = hasMaster && hasMusic && hasSfx;

            if (!m_MixerUsable)
            {
                Debug.LogWarning($"[AudioManager] Mixer is missing exposed parameters. " +
                                 $"Master: {hasMaster}, Music: {hasMusic}, Sfx: {hasSfx}.");
            }
        }

        /// <summary>Returns whether the mixer exposes the given parameter.</summary>
        private bool HasParameter(string name)
        {
            return m_Mixer != null && m_Mixer.GetFloat(name, out _);
        }

        #endregion
    }
}
