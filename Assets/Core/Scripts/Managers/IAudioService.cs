using PowerPulse.Audio;
using UnityEngine;

namespace PowerPulse.Core
{
    /// <summary>
    /// Contract of the persistent audio service: music playback, SFX pool and
    /// volume control.
    /// </summary>
    public interface IAudioService : IGameService
    {
        #region MUSIC

        /// <summary>Plays a music track by ID using the catalog.</summary>
        /// <param name="id">Music track identifier.</param>
        void PlayMusic(MusicId id);

        /// <summary>Crossfades from the current music to the target music.</summary>
        /// <param name="targetId">Target music track identifier.</param>
        /// <param name="duration">Crossfade duration in seconds (default 0.5s).</param>
        void CrossfadeTo(MusicId targetId, float duration = 0.5f);

        /// <summary>Stops the currently playing music immediately.</summary>
        void StopMusic();

        /// <summary>Draws the next gameplay track from the shuffled deck.</summary>
        /// <returns>The next gameplay MusicId.</returns>
        MusicId GetNextGameplayTrack();

        #endregion

        #region SFX

        /// <summary>Plays a one-shot SFX by ID using the catalog (volume, pitch, mixer group).</summary>
        /// <param name="id">SFX identifier.</param>
        void PlaySfx(SfxId id);

        /// <summary>Plays a one-shot SFX with a specific clip (bypasses catalog).</summary>
        /// <param name="clip">SFX clip to play.</param>
        /// <param name="volume">Volume multiplier (0-1).</param>
        /// <param name="pitch">Pitch multiplier.</param>
        void PlaySfxRaw(AudioClip clip, float volume = 1f, float pitch = 1f);

        #endregion

        #region SPECIAL EFFECTS

        /// <summary>
        /// Updates the pitch of the CollectAllColors tick loop.
        /// Call every frame or on a timer with normalized time (0 = start, 1 = end).
        /// Pitch lerps from 1.0 to 2.5.
        /// </summary>
        /// <param name="normalizedTime">Normalized boost progress (0 to 1).</param>
        void SetCollectAllColorsTick(float normalizedTime);

        /// <summary>Starts the CollectAllColors tick loop (plays the tick SFX looping).</summary>
        void StartCollectAllColorsTick();

        /// <summary>Stops the CollectAllColors tick loop.</summary>
        void StopCollectAllColorsTick();

        /// <summary>
        /// Plays the full death sequence: crossfades to death music,
        /// then plays Death_Hit -> Death_Inflate -> Death_Explode with delays.
        /// </summary>
        void PlayDeathSequence();

        #endregion

        #region VOLUME

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

        #endregion
    }
}
