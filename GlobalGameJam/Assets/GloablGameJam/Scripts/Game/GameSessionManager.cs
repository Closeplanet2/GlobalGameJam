using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GloablGameJam.Scripts.Game
{
    /// <summary>
    /// Persistent run state: lives, unique keys per level, and scene progression.
    /// </summary>
    public sealed class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        [Header("Lives")]
        [SerializeField, Min(1)] private int maxLives = 3;
        [SerializeField, Min(0f)] private float respawnDelaySeconds = 1.0f;

        [Header("Keys")]
        [SerializeField, Min(0)] private int requiredKeys = 3;

        [Header("Scenes")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string firstClipSceneName = "Clip_01";
        [SerializeField] private string finalLevelSceneName = "Level_05";

        [Header("Scene Rules")]
        [Tooltip("Only scenes starting with this prefix will reset key progress.")]
        [SerializeField] private string levelScenePrefix = "Level_";

        private int _livesRemaining;
        private readonly HashSet<string> _unlockedKeys = new(StringComparer.Ordinal);

        public int LivesRemaining => _livesRemaining;
        public int KeysUnlocked => _unlockedKeys.Count;
        public int KeysRequired => requiredKeys;

        public event Action<int> LivesChanged;
        public event Action<int, int> KeysChanged; // unlocked, required
        public event Action GameOver;
        public event Action LevelCompleted;

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

            // Safe defaults (menu boot)
            _livesRemaining = maxLives;
            _unlockedKeys.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        /// <summary>
        /// Called by the menu Play button. Starts a fresh run and loads Clip_01.
        /// </summary>
        public void StartNewRun()
        {
            _livesRemaining = maxLives;
            _unlockedKeys.Clear();

            LivesChanged?.Invoke(_livesRemaining);
            KeysChanged?.Invoke(KeysUnlocked, requiredKeys);

            SceneManager.LoadScene(firstClipSceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Menu: nothing to reset besides keys display if you want UI to show 0/required
            if (string.Equals(scene.name, mainMenuSceneName, StringComparison.OrdinalIgnoreCase))
            {
                _unlockedKeys.Clear();
                KeysChanged?.Invoke(KeysUnlocked, requiredKeys);
                return;
            }

            // Only reset keys when entering a LEVEL scene (not clips).
            if (scene.name.StartsWith(levelScenePrefix, StringComparison.OrdinalIgnoreCase))
            {
                _unlockedKeys.Clear();
                KeysChanged?.Invoke(KeysUnlocked, requiredKeys);
            }
        }

        /// <summary>
        /// Unlocks a unique key id. When enough keys are unlocked, completes the level.
        /// </summary>
        public bool TryUnlockKey(string keyId)
        {
            if (string.IsNullOrWhiteSpace(keyId)) return false;

            if (!_unlockedKeys.Add(keyId)) return false;

            KeysChanged?.Invoke(KeysUnlocked, requiredKeys);

            if (requiredKeys > 0 && KeysUnlocked >= requiredKeys)
            {
                LevelCompleted?.Invoke();
                LoadNextSceneOrMenu();
            }

            return true;
        }

        /// <summary>
        /// Called when the controlled body dies.
        /// </summary>
        public void NotifyPlayerDied(PlayerRespawnController respawner)
        {
            if (respawner == null) throw new ArgumentNullException(nameof(respawner));

            _livesRemaining = Mathf.Max(0, _livesRemaining - 1);
            LivesChanged?.Invoke(_livesRemaining);

            if (_livesRemaining <= 0)
            {
                GameOver?.Invoke();
                LoadMainMenu();
                return;
            }

            respawner.BeginRespawn(respawnDelaySeconds);
        }

        public void LoadMainMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void LoadNextSceneOrMenu()
        {
            var active = SceneManager.GetActiveScene();

            // If the final level is completed, return to menu.
            if (string.Equals(active.name, finalLevelSceneName, StringComparison.OrdinalIgnoreCase))
            {
                LoadMainMenu();
                return;
            }

            var nextIndex = active.buildIndex + 1;

            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                LoadMainMenu();
                return;
            }

            SceneManager.LoadScene(nextIndex);
        }
    }
}