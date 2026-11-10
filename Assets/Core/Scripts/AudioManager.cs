using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Persistent audio service. Hosts the looping music AudioSource and the SFX
    /// pool. Created by the Bootstraper on a persistent GameObject because it
    /// needs Unity audio resources. The full pool/volume implementation is
    /// completed in Phase 13.
    /// </summary>
    public class AudioManager : MonoBehaviour, IAudioService
    {
        #region PUBLIC METHODS

        /// <inheritdoc />
        public void PlayMusic(AudioClip clip)
        {
            // TODO (Phase 13): play the clip on the looping music AudioSource.
        }

        /// <inheritdoc />
        public void PlaySFX(AudioClip clip)
        {
            // TODO (Phase 13): grab an AudioSource from the pool and play the clip.
        }

        /// <inheritdoc />
        public void SetMasterVolume(float volume)
        {
            // TODO (Phase 13): apply the master volume.
        }

        /// <inheritdoc />
        public void SetMusicVolume(float volume)
        {
            // TODO (Phase 13): apply the music volume.
        }

        /// <inheritdoc />
        public void SetSfxVolume(float volume)
        {
            // TODO (Phase 13): apply the SFX volume.
        }

        /// <inheritdoc />
        public void SetMuted(bool muted)
        {
            // TODO (Phase 13): mute/unmute the audio output.
        }

        #endregion
    }
}
