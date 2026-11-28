using UnityEngine;
using UnityEngine.Audio;

namespace PowerPulse.Audio
{
    /// <summary>
    /// Central data container for all audio clips and their default settings.
    /// Created as a ScriptableObject asset and referenced by BootstrapConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "PowerPulse/Audio/Audio Catalog", fileName = "AudioCatalog")]
    public class SO_AudioCatalog : ScriptableObject
    {
        #region MUSIC

        [Header("Music Tracks")]
        [SerializeField, Tooltip("All music tracks in the game. Order: Menu, Gameplay_1..3, Death.")]
        private MusicTrack[] m_MusicTracks = new MusicTrack[5];

        #endregion

        #region SFX

        [Header("SFX Entries")]
        [SerializeField, Tooltip("All SFX entries with default volume, pitch and mixer group.")]
        private SfxEntry[] m_SfxEntries = new SfxEntry[16];

        #endregion

        #region PROPERTIES

        /// <summary>All music tracks defined in the catalog.</summary>
        public MusicTrack[] MusicTracks => m_MusicTracks;

        /// <summary>All SFX entries defined in the catalog.</summary>
        public SfxEntry[] SfxEntries => m_SfxEntries;

        #endregion

        #region HELPERS

        /// <summary>Returns the music track for the given ID, or null if not found.</summary>
        public MusicTrack GetMusicTrack(MusicId id)
        {
            for (int i = 0; i < m_MusicTracks.Length; i++)
            {
                if (m_MusicTracks[i].Id == id)
                {
                    return m_MusicTracks[i];
                }
            }
            return null;
        }

        /// <summary>Returns the SFX entry for the given ID, or null if not found.</summary>
        public SfxEntry GetSfxEntry(SfxId id)
        {
            for (int i = 0; i < m_SfxEntries.Length; i++)
            {
                if (m_SfxEntries[i].Id == id)
                {
                    return m_SfxEntries[i];
                }
            }
            return null;
        }

        #endregion
    }

    /// <summary>
    /// Music track data: clip, loop flag and default volume.
    /// </summary>
    [System.Serializable]
    public class MusicTrack
    {
        [Tooltip("Identifier used by the audio system.")]
        public MusicId Id = MusicId.Menu;

        [Tooltip("Audio clip to play.")]
        public AudioClip Clip;

        [Tooltip("Whether the track should loop.")]
        public bool Loop = true;

        [Range(0f, 1f), Tooltip("Default volume multiplier (0-1). Applied on top of the mixer Music volume.")]
        public float DefaultVolume = 1f;
    }

    /// <summary>
    /// SFX entry data: clip, default volume, default pitch and mixer group.
    /// </summary>
    [System.Serializable]
    public class SfxEntry
    {
        [Tooltip("Identifier used by the audio system.")]
        public SfxId Id = SfxId.UI_Click;

        [Tooltip("Audio clip to play.")]
        public AudioClip Clip;

        [Range(0f, 1f), Tooltip("Default volume multiplier (0-1). Applied on top of the mixer SFX volume.")]
        public float DefaultVolume = 1f;

        [Range(-3f, 3f), Tooltip("Default pitch multiplier. 1 = normal, >1 = higher/faster, <1 = lower/slower.")]
        public float DefaultPitch = 1f;

        [Tooltip("Mixer group to route this SFX through (typically the SFX group).")]
        public AudioMixerGroup MixerGroup;
    }
}