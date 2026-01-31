using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using CustomLibrary.Scripts.Instance;
using UnityEngine;

namespace CustomLibrary.Scripts.DreamfireMusicManager
{
    public abstract class DreamfireMusicManager<TMusicKey> : MonoBehaviourInstance<DreamfireMusicManager<TMusicKey> >
    {
        [Header("Audio Source Prefab")]
        [SerializeField] private AudioSource audioSourcePrefab;

        [Header("Initial Music Clip")]
        [SerializeField] private DreamfireMusicClip startClip;

        [Header("Music Source Settings")]
        [SerializeField] private DreamfireAudioSourceConfig musicConfig = new();
        [SerializeField] private DreamfireAudioSourceConfig effectConfig = new();

        [Header("Music Key Library")]
        [SerializeField] private SerializedDictionary<TMusicKey, DreamfireMusicClip> musicKeyLibrary;

        private AudioSource _musicSource;
        private AudioSource _effectSource;
        private readonly List<AudioSource> _localSources = new();

        public DreamfireAudioSourceConfig MusicConfig => musicConfig;

        public DreamfireAudioSourceConfig EffectConfig => effectConfig;

        private void Start()
        {
            if (audioSourcePrefab == null)
            {
                Debug.LogError("[DreamfireMusicManager] No AudioSource prefab assigned. Music system cannot function.");
                return;
            }

            LoadPreferences();

            _musicSource = CreateConfiguredSource(musicConfig);
            _effectSource = CreateConfiguredSource(effectConfig);

            if (startClip != null)
            {
                PlayGlobal(startClip);
            }
        }

        private void Update()
        {
            for (int i = _localSources.Count - 1; i >= 0; i--)
            {
                AudioSource source = _localSources[i];

                if (source == null || !source.isPlaying)
                {
                    if (source != null)
                    {
                        Destroy(source.gameObject);
                    }

                    _localSources.RemoveAt(i);
                }
            }
        }

        private void OnApplicationQuit()
        {
            SavePreferences();
        }

        public void PlayGlobal(TMusicKey musicKey) => PlayGlobal(musicKeyLibrary.GetValueOrDefault(musicKey, null));

        public void PlayGlobal(DreamfireMusicClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[DreamfireMusicManager] Msuic Clip is null.");
                return;
            }

            AudioSource target = clip.IsMusic ? _musicSource : _effectSource;

            if (target == null)
            {
                Debug.LogWarning("[DreamfireMusicManager] Global AudioSource is missing.");
                return;
            }

            Debug.Log($"Playing Sound: {clip.name}");
            clip.Play(target);
        }

        public void StopClip(DreamfireMusicClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (clip.IsMusic)
            {
                _musicSource?.Stop();
            }
            else
            {
                _effectSource?.Stop();
            }
        }

        public void PlayLocal(DreamfireMusicClip clip, Vector3 position, Transform parent = null)
        {
            if (clip == null)
            {
                return;
            }

            if (audioSourcePrefab == null)
            {
                Debug.LogError("[DreamfireMusicManager] Cannot play local clip: AudioSource prefab is missing.");
                return;
            }

            Transform parentTransform = parent != null ? parent : transform.parent;
            AudioSource instance = Instantiate(audioSourcePrefab, position, Quaternion.identity, parentTransform);

            clip.Play(instance);
            _localSources.Add(instance);
        }

        public void SetMusicConfigVolume(float value)
        {
            musicConfig.volume = value;

            if (_musicSource != null)
            {
                _musicSource.volume = value;
            }

            PlayerPrefs.SetFloat(DreamfireMusicManagerPrefKeys.BackgroundMusic.ToString(), value);
            PlayerPrefs.Save();
        }

        public void SetEffectConfigVolume(float value)
        {
            effectConfig.volume = value;

            if (_effectSource != null)
            {
                _effectSource.volume = value;
            }

            PlayerPrefs.SetFloat(DreamfireMusicManagerPrefKeys.SoundEffects.ToString(), value);
            PlayerPrefs.Save();
        }

        private AudioSource CreateConfiguredSource(DreamfireAudioSourceConfig config)
        {
            AudioSource source = Instantiate(audioSourcePrefab, transform);
            config.ApplyTo(source);
            return source;
        }

        private void LoadPreferences()
        {
            musicConfig.volume = PlayerPrefs.GetFloat(DreamfireMusicManagerPrefKeys.BackgroundMusic.ToString(), musicConfig.volume);
            effectConfig.volume = PlayerPrefs.GetFloat(DreamfireMusicManagerPrefKeys.SoundEffects.ToString(), effectConfig.volume);
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetFloat(DreamfireMusicManagerPrefKeys.BackgroundMusic.ToString(), musicConfig.volume);
            PlayerPrefs.SetFloat(DreamfireMusicManagerPrefKeys.SoundEffects.ToString(), effectConfig.volume);
            PlayerPrefs.Save();
        }
    }
}