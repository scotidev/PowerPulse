using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using PowerPulse.Audio;

namespace PowerPulse.Core
{
    /// <summary>
    /// Persistent audio service. Hosts dual music AudioSources for crossfading,
    /// a pool of AudioSources for one-shot SFX, a dedicated tick source for
    /// CollectAllColors boost, and routes everything through an
    /// <see cref="AudioMixer"/> with exposed volume parameters.
    /// Created by the Bootstraper on a persistent GameObject.
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
        private const int c_SfxPoolSize = 8;

        /// <summary>Default crossfade duration in seconds.</summary>
        private const float c_DefaultCrossfadeDuration = 0.5f;

        /// <summary>Pitch range for CollectAllColors tick (start -> end).</summary>
        private const float c_TickPitchStart = 1f;
        private const float c_TickPitchEnd = 2.5f;

        /// <summary>Delays between death SFX sequence (seconds).</summary>
        private const float c_DeathHitToInflateDelay = 0.15f;
        private const float c_DeathInflateToExplodeDelay = 0.2f;

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

        [Header("Music Sources (Dual for Crossfade)")]
        [SerializeField, Tooltip("AudioSource A for music crossfading. Created at runtime when empty.")]
        private AudioSource m_MusicSourceA;

        [SerializeField, Tooltip("AudioSource B for music crossfading. Created at runtime when empty.")]
        private AudioSource m_MusicSourceB;

        [Header("SFX Pool")]
        [SerializeField, Tooltip("Number of AudioSources pooled for one-shot SFX.")]
        private int m_SfxPoolSize = c_SfxPoolSize;

        [Header("Catalog")]
        [SerializeField, Tooltip("Audio catalog with all clips and default settings. Set by Bootstraper.")]
        private SO_AudioCatalog m_Catalog;

        #endregion

        #region FIELDS

        private readonly List<AudioSource> m_SfxPool = new List<AudioSource>();
        private float m_LastMasterDecibels = c_MaxDecibels;
        private bool m_MixerUsable = true;

        // Music crossfade state
        private AudioSource m_ActiveMusicSource;
        private AudioSource m_NextMusicSource;
        private Coroutine m_CrossfadeRoutine;
        private MusicId m_CurrentMusicId = MusicId.Menu;

        // Gameplay music shuffle deck
        private readonly List<MusicId> m_GameplayDeck = new List<MusicId>
        {
            MusicId.Gameplay_1,
            MusicId.Gameplay_2,
            MusicId.Gameplay_3
        };
        private int m_DeckIndex = 0;

        // CollectAllColors tick
        private AudioSource m_TickSource;
        private bool m_TickPlaying = false;

        #endregion

        #region UNITY CALLBACKS

        private void Awake()
        {
            BuildSfxPool();
            CreateMusicSources();
            CreateTickSource();
            ValidateMixer();
        }

        private void OnDestroy()
        {
            Debug.Log($"[AudioManager] OnDestroy called");
            if (m_CrossfadeRoutine != null)
            {
                StopCoroutine(m_CrossfadeRoutine);
            }
        }

        #endregion

        #region INITIALIZATION

        /// <summary>
        /// Initializes the AudioManager with the catalog and mixer.
        /// Called by Bootstraper after instantiation.
        /// </summary>
        /// <param name="catalog">Audio catalog with all clips.</param>
        /// <param name="mixer">Audio mixer (optional, will load from Resources if null).</param>
        public void Initialize(SO_AudioCatalog catalog, AudioMixer mixer = null)
        {
            m_Catalog = catalog;
            if (mixer != null)
            {
                m_Mixer = mixer;
            }
            ValidateMixer();
        }

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

        private void CreateMusicSources()
        {
            if (m_MusicSourceA == null)
            {
                m_MusicSourceA = gameObject.AddComponent<AudioSource>();
            }
            m_MusicSourceA.loop = true;
            m_MusicSourceA.playOnAwake = false;

            if (m_MusicSourceB == null)
            {
                m_MusicSourceB = gameObject.AddComponent<AudioSource>();
            }
            m_MusicSourceB.loop = true;
            m_MusicSourceB.playOnAwake = false;
            m_MusicSourceB.volume = 0f; // Starts silent for crossfade

            m_ActiveMusicSource = m_MusicSourceA;
            m_NextMusicSource = m_MusicSourceB;
        }

        private void CreateTickSource()
        {
            m_TickSource = gameObject.AddComponent<AudioSource>();
            m_TickSource.loop = true;
            m_TickSource.playOnAwake = false;
            m_TickSource.volume = 1f; // Volume controlled by mixer SFX group
        }

        private void ValidateMixer()
        {
            if (m_Mixer == null)
            {
                m_Mixer = Resources.Load<AudioMixer>("Audio/PowerPulseAudio");
            }

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
                Debug.LogWarning($"[AudioManager] Mixer is missing exposed parameters. Master: {hasMaster}, Music: {hasMusic}, Sfx: {hasSfx}.");
            }
        }

        private bool HasParameter(string name)
        {
            return m_Mixer != null && m_Mixer.GetFloat(name, out _);
        }

        #endregion

        #region PUBLIC METHODS - MUSIC

        public void PlayMusic(MusicId id)
        {
            if (m_Catalog == null)
            {
                Debug.LogError($"[AudioManager] PlayMusic FAILED: Catalog not initialized!");
                return;
            }

            var track = m_Catalog.GetMusicTrack(id);
            if (track == null || track.Clip == null)
            {
                Debug.LogError($"[AudioManager] PlayMusic FAILED: Music track {id} not found or clip missing in catalog!");
                return;
            }

            // Stop any ongoing crossfade
            if (m_CrossfadeRoutine != null)
            {
                StopCoroutine(m_CrossfadeRoutine);
                m_CrossfadeRoutine = null;
            }

            // Swap sources: active becomes next, next becomes active
            SwapMusicSources();

            m_ActiveMusicSource.clip = track.Clip;
            m_ActiveMusicSource.loop = track.Loop;
            m_ActiveMusicSource.volume = track.DefaultVolume;
            m_ActiveMusicSource.Play();

            m_NextMusicSource.Stop();
            m_NextMusicSource.volume = 0f;

            m_CurrentMusicId = id;
        }

        public void CrossfadeTo(MusicId targetId, float duration = c_DefaultCrossfadeDuration)
        {
            if (m_Catalog == null)
            {
                Debug.LogError($"[AudioManager] CrossfadeTo FAILED: Catalog not initialized!");
                return;
            }

            var track = m_Catalog.GetMusicTrack(targetId);
            if (track == null || track.Clip == null)
            {
                Debug.LogError($"[AudioManager] CrossfadeTo FAILED: Music track {targetId} not found or clip missing in catalog!");
                return;
            }

            if (m_CrossfadeRoutine != null)
            {
                StopCoroutine(m_CrossfadeRoutine);
            }

            m_CrossfadeRoutine = StartCoroutine(CrossfadeRoutine(track, duration));
        }

        public void StopMusic()
        {
            if (m_CrossfadeRoutine != null)
            {
                StopCoroutine(m_CrossfadeRoutine);
                m_CrossfadeRoutine = null;
            }

            m_ActiveMusicSource.Stop();
            m_NextMusicSource.Stop();
            m_ActiveMusicSource.volume = 0f;
            m_NextMusicSource.volume = 0f;
        }

        public MusicId GetNextGameplayTrack()
        {
            if (m_DeckIndex >= m_GameplayDeck.Count)
            {
                ShuffleDeck();
            }

            MusicId next = m_GameplayDeck[m_DeckIndex];
            m_DeckIndex++;
            return next;
        }

        #endregion

        #region PUBLIC METHODS - SFX

        public void PlaySfx(SfxId id)
        {
            if (m_Catalog == null)
            {
                Debug.LogWarning("[AudioManager] PlaySfx: Catalog not initialized.");
                return;
            }

            var entry = m_Catalog.GetSfxEntry(id);
            if (entry == null || entry.Clip == null)
            {
                Debug.LogWarning($"[AudioManager] PlaySfx: SFX {id} not found or clip missing in catalog.");
                return;
            }

            PlaySfxRaw(entry.Clip, entry.DefaultVolume, entry.DefaultPitch, entry.MixerGroup);
        }

        public void PlaySfxRaw(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            PlaySfxRaw(clip, volume, pitch, null);
        }

        public void PlaySfxRaw(AudioClip clip, float volume = 1f, float pitch = 1f, AudioMixerGroup mixerGroup = null)
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
            source.volume = volume;
            source.pitch = pitch;
            source.outputAudioMixerGroup = mixerGroup;
            source.Play();
        }

        #endregion

        #region PUBLIC METHODS - SPECIAL EFFECTS

        public void SetCollectAllColorsTick(float normalizedTime)
        {
            if (!m_TickPlaying || m_TickSource == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(normalizedTime);
            float pitch = Mathf.Lerp(c_TickPitchStart, c_TickPitchEnd, clamped);
            m_TickSource.pitch = pitch;
        }

        public void StartCollectAllColorsTick()
        {
            if (m_Catalog == null)
            {
                Debug.LogWarning("[AudioManager] Catalog not initialized.");
                return;
            }

            var entry = m_Catalog.GetSfxEntry(SfxId.CollectAllColors_Tick);
            if (entry == null || entry.Clip == null)
            {
                Debug.LogWarning("[AudioManager] CollectAllColors_Tick not found in catalog.");
                return;
            }

            m_TickSource.clip = entry.Clip;
            m_TickSource.loop = true;
            m_TickSource.pitch = c_TickPitchStart;
            m_TickSource.outputAudioMixerGroup = entry.MixerGroup;
            m_TickSource.volume = entry.DefaultVolume;
            m_TickSource.Play();

            m_TickPlaying = true;
        }

        public void StopCollectAllColorsTick()
        {
            if (m_TickSource != null && m_TickPlaying)
            {
                m_TickSource.Stop();
                m_TickPlaying = false;
            }
        }

        public void PlayDeathSequence()
        {
            // 1. Crossfade to death music
            CrossfadeTo(MusicId.Death, c_DefaultCrossfadeDuration);

            // 2. Play death SFX sequence with delays
            StartCoroutine(DeathSequenceRoutine());
        }

        #endregion

        #region PUBLIC METHODS - VOLUME

        public void SetMasterVolume(float volume)
        {
            SetDecibels(m_MasterParameter, ToDecibels(volume));
        }

        public void SetMusicVolume(float volume)
        {
            SetDecibels(m_MusicParameter, ToDecibels(volume));
        }

        public void SetSfxVolume(float volume)
        {
            SetDecibels(m_SfxParameter, ToDecibels(volume));
        }

        public void SetMuted(bool muted)
        {
            if (m_Mixer == null || !m_MixerUsable)
            {
                return;
            }

            m_Mixer.SetFloat(m_MasterParameter, muted ? c_MutedDecibels : m_LastMasterDecibels);
        }

        #endregion

        #region PRIVATE METHODS - MUSIC HELPERS

        private void SwapMusicSources()
        {
            (m_ActiveMusicSource, m_NextMusicSource) = (m_NextMusicSource, m_ActiveMusicSource);
        }

        private IEnumerator CrossfadeRoutine(MusicTrack targetTrack, float duration)
        {
            // Prepare next source
            m_NextMusicSource.clip = targetTrack.Clip;
            m_NextMusicSource.loop = targetTrack.Loop;
            m_NextMusicSource.volume = 0f;
            m_NextMusicSource.Play();

            float elapsed = 0f;
            float startVolumeA = m_ActiveMusicSource.volume;
            float targetVolumeB = targetTrack.DefaultVolume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaled time so crossfade works during pause
                float t = Mathf.Clamp01(elapsed / duration);

                // Ease out for smoother crossfade
                float eased = 1f - (1f - t) * (1f - t);

                m_ActiveMusicSource.volume = Mathf.Lerp(startVolumeA, 0f, eased);
                m_NextMusicSource.volume = Mathf.Lerp(0f, targetVolumeB, eased);

                yield return null;
            }

            // Ensure exact final values
            m_ActiveMusicSource.volume = 0f;
            m_NextMusicSource.volume = targetVolumeB;

            // Swap references so active is now the new track
            SwapMusicSources();
            m_CurrentMusicId = targetTrack.Id;

            m_CrossfadeRoutine = null;
        }

        private void ShuffleDeck()
        {
            // Fisher-Yates shuffle
            for (int i = m_GameplayDeck.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (m_GameplayDeck[i], m_GameplayDeck[j]) = (m_GameplayDeck[j], m_GameplayDeck[i]);
            }
            m_DeckIndex = 0;
        }

        #endregion

        #region PRIVATE METHODS - SPECIAL EFFECTS

        private IEnumerator DeathSequenceRoutine()
        {
            // Wait a tiny bit for crossfade to start
            yield return new WaitForSecondsRealtime(0.1f);

            // Death_Hit
            PlaySfx(SfxId.Death_Hit);

            yield return new WaitForSecondsRealtime(c_DeathHitToInflateDelay);

            // Death_Inflate
            PlaySfx(SfxId.Death_Inflate);

            yield return new WaitForSecondsRealtime(c_DeathInflateToExplodeDelay);

            // Death_Explode
            PlaySfx(SfxId.Death_Explode);
        }

        #endregion

        #region PRIVATE METHODS - SFX POOL

        private AudioSource GetFreeSfxSource()
        {
            for (int i = 0; i < m_SfxPool.Count; i++)
            {
                if (!m_SfxPool[i].isPlaying)
                {
                    return m_SfxPool[i];
                }
            }

            // Pool exhausted - expand dynamically (rare)
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            m_SfxPool.Add(newSource);
            return newSource;
        }

        #endregion

        #region PRIVATE METHODS - VOLUME HELPERS

        private float ToDecibels(float normalized)
        {
            float clamped = Mathf.Clamp01(normalized);
            return Mathf.Log10(Mathf.Max(clamped, c_VolumeFloor)) * 20f;
        }

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

        #endregion
    }
}
