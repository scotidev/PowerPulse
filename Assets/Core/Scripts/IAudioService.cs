using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Contract of the persistent audio service: music playback, SFX pool and
    /// volume control.
    /// </summary>
    public interface IAudioService : IGameService
    {
        /// <summary>Plays a music clip in loop on the dedicated AudioSource.</summary>
        /// <param name="clip">Music clip to play.</param>
        void PlayMusic(AudioClip clip);

        /// <summary>Plays a one-shot SFX using an AudioSource from the pool.</summary>
        /// <param name="clip">SFX clip to play.</param>
        void PlaySFX(AudioClip clip);

        /// <summary>Sets the master volume.</summary>
        /// <param name="volume">Volume normalized between 0 and 1.</param>
        void SetMasterVolume(float volume);

        /// <summary>Sets the music volume.</summary>
        /// <param name="volume">Volume normalized between 0 and 1.</param>
        void SetMusicVolume(float volume);

        /// <summary>Sets the SFX volume.</summary>
        /// <param name="volume">Volume normalized between 0 and 1.</param>
        void SetSfxVolume(float volume);

        /// <summary>Enables or disables the audio output.</summary>
        /// <param name="muted">True to mute, false to unmute.</param>
        void SetMuted(bool muted);
    }
}
