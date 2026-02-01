using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GloablGameJam.Scripts.Game
{
    /// <summary>
    /// Persistent run/session manager.
    ///
    /// Rules:
    /// - Hearts persist across clip + level scenes until the run ends.
    /// - Keys reset to 0 whenever a level scene loads/reloads.
    /// - On player death: lose 1 heart; if hearts remain, reload current scene; if 0, go to MainMenu.
    /// - When keys reach requiredKeys in a level: advance to next scene.
    /// - When final level completes: go back to MainMenu.
    /// - Run timer starts on Play and persists across all scenes in the run.
    /// </summary>
    public sealed class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        [Header("Run Rules")]
        [SerializeField, Min(1)] private int maxHearts = 3;
        [SerializeField, Min(0)] private int requiredKeys = 3;

        [Header("Scenes")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string firstClipSceneName = "Clip_01";
        [SerializeField] private string finalLevelSceneName = "Level_05";

        [Header("Scene Rules")]
        [Tooltip("Only scenes starting with this prefix are considered gameplay levels (keys reset here).")]
        [SerializeField] private string levelScenePrefix = "Level_";

        [Header("Run Timer")]
        [SerializeField] private bool trackRunTime = true;

        [Header("SFX")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip loseHeartClip;
        [SerializeField] private AudioClip gainKeyClip;
        [SerializeField] private AudioClip levelCompleteClip;
        [SerializeField] private AudioClip gameOverClip;

        private int _heartsRemaining;
        private readonly HashSet<string> _unlockedKeys = new(StringComparer.Ordinal);

        private bool _isTransitioningToMenu;

        private float _runTimeSeconds;
        private bool _runTimerActive;

        public int HeartsRemaining => _heartsRemaining;
        public int KeysUnlocked => _unlockedKeys.Count;
        public int KeysRequired => requiredKeys;

        public float RunTimeSeconds => _runTimeSeconds;

        public event Action<int> HeartsChanged;
        public event Action<int, int> KeysChanged; // unlocked, required
        public event Action<float> RunTimeUpdated;
        public event Action LevelCompleted;
        public event Action GameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;

            // Default safe state (menu).
            _heartsRemaining = maxHearts;
            _unlockedKeys.Clear();
            _isTransitioningToMenu = false;

            _runTimeSeconds = 0f;
            _runTimerActive = false;

            // Useful to detect duplicates (should log only once per app session).
            Debug.Log($"[GameSessionManager] Awake in scene '{SceneManager.GetActiveScene().name}'", this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void Update()
        {
            if (!_runTimerActive || !trackRunTime) return;

            _runTimeSeconds += Time.unscaledDeltaTime;
            RunTimeUpdated?.Invoke(_runTimeSeconds);
        }

        /// <summary>
        /// Called by Main Menu Play button.
        /// Resets run state and loads the first clip scene.
        /// </summary>
        public void StartNewRun()
        {
            _isTransitioningToMenu = false;

            _heartsRemaining = maxHearts;
            _unlockedKeys.Clear();

            _runTimeSeconds = 0f;
            _runTimerActive = true;

            HeartsChanged?.Invoke(_heartsRemaining);
            KeysChanged?.Invoke(KeysUnlocked, requiredKeys);
            RunTimeUpdated?.Invoke(_runTimeSeconds);

            SceneManager.LoadScene(firstClipSceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Keys reset whenever a level scene loads/reloads.
            if (IsLevelScene(scene.name))
            {
                _unlockedKeys.Clear();
                KeysChanged?.Invoke(KeysUnlocked, requiredKeys);
            }

            // Menu: show 0 keys, stop timer.
            if (string.Equals(scene.name, mainMenuSceneName, StringComparison.OrdinalIgnoreCase))
            {
                _unlockedKeys.Clear();
                KeysChanged?.Invoke(KeysUnlocked, requiredKeys);

                _isTransitioningToMenu = false;
                _runTimerActive = false;
            }

            // Always refresh hearts/timer for HUD after any scene load.
            HeartsChanged?.Invoke(_heartsRemaining);
            RunTimeUpdated?.Invoke(_runTimeSeconds);
        }

        public bool IsLevelScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) &&
                   sceneName.StartsWith(levelScenePrefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Unlocks a unique key id for the current level.
        /// When enough keys are unlocked, completes the level and advances.
        /// </summary>
        public bool TryUnlockKey(string keyId)
        {
            if (_isTransitioningToMenu) return false;
            if (string.IsNullOrWhiteSpace(keyId)) return false;

            if (!_unlockedKeys.Add(keyId)) return false;

            KeysChanged?.Invoke(KeysUnlocked, requiredKeys);
            PlaySfx(gainKeyClip);

            if (requiredKeys > 0 && KeysUnlocked >= requiredKeys)
            {
                LevelCompleted?.Invoke();
                PlaySfx(levelCompleteClip);
                LoadNextSceneOrMenu();
            }

            return true;
        }

        /// <summary>
        /// Player death: lose a heart. If hearts remain, reload current scene.
        /// If no hearts remain, go back to main menu and end the run.
        /// </summary>
        public void NotifyPlayerDied()
        {
            if (_isTransitioningToMenu) return;

            _heartsRemaining = Mathf.Max(0, _heartsRemaining - 1);
            HeartsChanged?.Invoke(_heartsRemaining);
            PlaySfx(loseHeartClip);

            if (_heartsRemaining <= 0)
            {
                _isTransitioningToMenu = true;
                _runTimerActive = false;

                GameOver?.Invoke();
                PlaySfx(gameOverClip);

                // Defer to next frame so no other reload call can "win".
                StartCoroutine(LoadMainMenuNextFrame());
                return;
            }

            ReloadActiveScene();
        }

        private IEnumerator LoadMainMenuNextFrame()
        {
            yield return null;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void LoadMainMenu()
        {
            _isTransitioningToMenu = true;
            _runTimerActive = false;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void ReloadActiveScene()
        {
            var active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }

        private void LoadNextSceneOrMenu()
        {
            var active = SceneManager.GetActiveScene();

            if (string.Equals(active.name, finalLevelSceneName, StringComparison.OrdinalIgnoreCase))
            {
                _runTimerActive = false;
                LoadMainMenu();
                return;
            }

            var nextIndex = active.buildIndex + 1;

            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                _runTimerActive = false;
                LoadMainMenu();
                return;
            }

            SceneManager.LoadScene(nextIndex);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            if (sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }
    }
}