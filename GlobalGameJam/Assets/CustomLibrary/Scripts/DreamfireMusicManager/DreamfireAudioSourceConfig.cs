using System;
using UnityEngine;

namespace CustomLibrary.Scripts.DreamfireMusicManager
{
    [Serializable]
    public class DreamfireAudioSourceConfig
    {
        /// <summary>
        /// Priority of the audio source (0 = highest priority, 256 = lowest).
        /// </summary>
        public int priority = 128;

        /// <summary>
        /// Volume of the audio source (0.0 = silent, 1.0 = full volume).
        /// </summary>
        public float volume = 1f;

        /// <summary>
        /// Pitch multiplier of the audio source (default = 1.0).
        /// </summary>
        public float pitch = 1f;

        /// <summary>
        /// Stereo pan position (-1.0 = full left, 1.0 = full right).
        /// </summary>
        public float stereoPan = 0f;

        /// <summary>
        /// Spatial blend (0.0 = 2D sound, 1.0 = fully 3D).
        /// </summary>
        public float spatialBlend = 0f;

        /// <summary>
        /// Reverb zone mix factor (0.0 = no reverb, 1.0 = full effect).
        /// </summary>
        public float reverbZoneMix = 1f;

        /// <summary>
        /// Applies this configuration to the given <see cref="AudioSource"/>.
        /// </summary>
        /// <param name="source">The audio source to configure.</param>
        public void ApplyTo(AudioSource source)
        {
            if (source == null)
            {
                Debug.LogError("[DreamfireAudioSourceConfig] Cannot apply settings: AudioSource is null.");
                return;
            }

            source.priority = priority;
            source.volume = volume;
            source.pitch = pitch;
            source.panStereo = stereoPan;
            source.spatialBlend = spatialBlend;
            source.reverbZoneMix = reverbZoneMix;
        }
    }
}