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
    /// Run rules (as requested):
    /// - Start of run: 3 hearts, 0 keys, timer = 0.
    /// - Hearts persist across level changes within the same run.
    /// - Keys reset to 0 whenever a LEVEL scene loads/reloads (clip scenes do not affect keys).
    /// - On death: lose 1 heart and reload the current scene.
    /// - If hearts reach 0: go back to MainMenu (and end the run).
    /// - If keys reach requiredKeys: advance to next scene.
    /// - If final level is completed: go back to MainMenu (end run).
    /// - Run timer pauses during clip scenes (ClipSceneController calls SetRunTimerPaused).
    ///
    /// Notes:
    /// - This manager should exist ONCE (usually in MainMenu) and uses DontDestroyOnLoad.
    /// - UI should listen to events and/or force-refresh on enable/scene load.
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

        [Header("Scene Classification")]
        [Tooltip("Only scenes starting with this prefix are considered gameplay levels (keys reset here).")]
        [SerializeField] private string levelScenePrefix = "Level_";
        [Tooltip("Optional: treat scenes starting with this prefix as clip scenes (timer auto-pauses).")]
        [SerializeField] private string clipScenePrefix = "Clip_";

        [Header("Timer")]
        [SerializeField] private bool trackRunTime = true;

        [Header("SFX")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip loseHeartClip;
        [SerializeField] private AudioClip gainKeyClip;
        [SerializeField] private AudioClip levelCompleteClip;
        [SerializeField] private AudioClip gameOverClip;

        private int _heartsRemaining;
        private readonly HashSet<string> _unlockedKeys = new(StringComparer.Ordinal);

        private float _runTimeSeconds;
        private bool _runTimerActive;
        private bool _runTimerPaused;

        // Guards to stop double-death / double-load issues.
        private bool _isSceneTransitionInProgress;
        private bool _isEndingRunToMenu;

        public int HeartsRemaining => _heartsRemaining;
        public int KeysUnlocked => _unlockedKeys.Count;
        public int KeysRequired => requiredKeys;

        public float RunTimeSeconds => _runTimeSeconds;
        public bool IsRunTimerPaused => _runTimerPaused;
        public bool IsRunActive => _runTimerActive;

        public event Action<int> HeartsChanged;
        public event Action<int, int> KeysChanged; // unlocked, required
        public event Action<float> RunTimeUpdated;
        public event Action<bool> RunTimerPausedChanged;

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

            // Safe default (menu state).
            ResetRunStateInternal(setTimerActive: false);
            _isSceneTransitionInProgress = false;
            _isEndingRunToMenu = false;

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
            if (!trackRunTime) return;
            if (!_runTimerActive) return;
            if (_runTimerPaused) return;

            _runTimeSeconds += Time.unscaledDeltaTime;
            RunTimeUpdated?.Invoke(_runTimeSeconds);
        }

        /// <summary>
        /// Main Menu "Play" button should call this.
        /// Fully resets the run, then loads the first clip scene.
        /// </summary>
        public void StartNewRun()
        {
            _isEndingRunToMenu = false;
            _isSceneTransitionInProgress = false;

            ResetRunStateInternal(setTimerActive: true);

            // Force UI refresh immediately.
            HeartsChanged?.Invoke(_heartsRemaining);
            KeysChanged?.Invoke(KeysUnlocked, requiredKeys);
            RunTimeUpdated?.Invoke(_runTimeSeconds);
            RunTimerPausedChanged?.Invoke(_runTimerPaused);

            SceneManager.LoadScene(firstClipSceneName);
        }

        /// <summary>
        /// Pause/unpause the run timer (used by ClipSceneController).
        /// </summary>
        public void SetRunTimerPaused(bool paused)
        {
            if (_runTimerPaused == paused) return;

            _runTimerPaused = paused;
            RunTimerPausedChanged?.Invoke(_runTimerPaused);

            // Push current time to keep HUD consistent.
            RunTimeUpdated?.Invoke(_runTimeSeconds);
        }

        /// <summary>
        /// Called when a key is earned (e.g. body swap into key NPC).
        /// When enough keys are unlocked for the level, advances to next scene.
        /// </summary>
        public bool TryUnlockKey(string keyId)
        {
            if (_isEndingRunToMenu) return false;
            if (_isSceneTransitionInProgress) return false;
            if (string.IsNullOrWhiteSpace(keyId)) return false;

            if (!_unlockedKeys.Add(keyId)) return false;

            KeysChanged?.Invoke(KeysUnlocked, requiredKeys);
            PlaySfx(gainKeyClip);

            if (requiredKeys > 0 && KeysUnlocked >= requiredKeys)
            {
                LevelCompleted?.Invoke();
                PlaySfx(levelCompleteClip);

                StartCoroutine(AdvanceNextFrame());
            }

            return true;
        }

        /// <summary>
        /// Called by GlobalGameJam when the controlled body dies.
        /// Applies heart loss and reloads the scene or ends the run.
        /// </summary>
        public void NotifyPlayerDied()
        {
            if (_isEndingRunToMenu) return;
            if (_isSceneTransitionInProgress) return;

            _heartsRemaining = Mathf.Max(0, _heartsRemaining - 1);
            HeartsChanged?.Invoke(_heartsRemaining);
            PlaySfx(loseHeartClip);

            if (_heartsRemaining <= 0)
            {
                EndRunToMenu();
                return;
            }

            StartCoroutine(ReloadSceneNextFrame());
        }

        public void LoadMainMenu()
        {
            EndRunToMenu();
        }

        private void EndRunToMenu()
        {
            if (_isEndingRunToMenu) return;

            _isEndingRunToMenu = true;
            _isSceneTransitionInProgress = true;

            _runTimerActive = false;
            _runTimerPaused = false;
            RunTimerPausedChanged?.Invoke(_runTimerPaused);

            GameOver?.Invoke();
            PlaySfx(gameOverClip);

            StartCoroutine(LoadMenuNextFrame());
        }

        private IEnumerator LoadMenuNextFrame()
        {
            yield return null;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private IEnumerator ReloadSceneNextFrame()
        {
            _isSceneTransitionInProgress = true;
            yield return null;

            var active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }

        private IEnumerator AdvanceNextFrame()
        {
            _isSceneTransitionInProgress = true;
            yield return null;

            var active = SceneManager.GetActiveScene();

            // If this is the final level, end run to menu.
            if (string.Equals(active.name, finalLevelSceneName, StringComparison.OrdinalIgnoreCase))
            {
                _runTimerActive = false;
                _runTimerPaused = false;
                RunTimerPausedChanged?.Invoke(_runTimerPaused);

                SceneManager.LoadScene(mainMenuSceneName);
                yield break;
            }

            var nextIndex = active.buildIndex + 1;
            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                _runTimerActive = false;
                _runTimerPaused = false;
                RunTimerPausedChanged?.Invoke(_runTimerPaused);

                SceneManager.LoadScene(mainMenuSceneName);
                yield break;
            }

            SceneManager.LoadScene(nextIndex);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _isSceneTransitionInProgress = false;

            // If we reached the menu, ensure run is not active and keys show 0.
            if (string.Equals(scene.name, mainMenuSceneName, StringComparison.OrdinalIgnoreCase))
            {
                _unlockedKeys.Clear();
                KeysChanged?.Invoke(KeysUnlocked, requiredKeys);

                _runTimerActive = false;
                _runTimerPaused = false;
                RunTimerPausedChanged?.Invoke(_runTimerPaused);

                HeartsChanged?.Invoke(_heartsRemaining);
                RunTimeUpdated?.Invoke(_runTimeSeconds);

                // Allow new run again.
                _isEndingRunToMenu = false;
                return;
            }

            // Auto-pause timer on clip scenes if you use a prefix naming convention.
            // (ClipSceneController can still explicitly call SetRunTimerPaused(true/false).)
            if (IsClipScene(scene.name))
            {
                SetRunTimerPaused(true);
            }

            // Reset keys only on LEVEL scenes (including reloads).
            if (IsLevelScene(scene.name))
            {
                _unlockedKeys.Clear();
                KeysChanged?.Invoke(KeysUnlocked, requiredKeys);
            }

            // Always push hearts/time so HUD can re-render after load.
            HeartsChanged?.Invoke(_heartsRemaining);
            RunTimeUpdated?.Invoke(_runTimeSeconds);
        }

        public bool IsLevelScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) &&
                   sceneName.StartsWith(levelScenePrefix, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsClipScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) &&
                   sceneName.StartsWith(clipScenePrefix, StringComparison.OrdinalIgnoreCase);
        }

        private void ResetRunStateInternal(bool setTimerActive)
        {
            _heartsRemaining = maxHearts;
            _unlockedKeys.Clear();

            _runTimeSeconds = 0f;
            _runTimerActive = setTimerActive;
            _runTimerPaused = false;
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            if (sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }
    }
}