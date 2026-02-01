using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GloablGameJam.Scripts.Game
{
    /// <summary>
    /// Clip scene controller: plays audio over a static image (optionally animated),
    /// then advances to the next scene. Supports skip using the new Input System.
    /// </summary>
    public sealed class ClipSceneController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool playOnStart = true;

        [Header("Timing")]
        [Tooltip("Extra time after audio finishes before advancing.")]
        [SerializeField, Min(0f)] private float postAudioDelaySeconds = 0.25f;

        [Tooltip("If audioSource/clip is missing, auto-advance after fallbackSeconds.")]
        [SerializeField] private bool useFallbackTimerIfNoAudio = true;

        [SerializeField, Min(0f)] private float fallbackSeconds = 3.0f;

        [Header("Skip")]
        [SerializeField] private bool allowSkip = true;

        [Tooltip("Skip when any key is pressed.")]
        [SerializeField] private bool skipOnAnyKey = true;

        [Tooltip("Skip when left mouse button is pressed.")]
        [SerializeField] private bool skipOnLeftClick = true;

        [Tooltip("Skip when these keys are pressed (in addition to any-key, if enabled).")]
        [SerializeField] private Key[] extraSkipKeys = { Key.Space, Key.Escape, Key.Enter };

        [Header("Optional Fade")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.25f;

        private bool _transitioning;
        private Coroutine _routine;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
        }

        private void Start()
        {
            if (playOnStart && audioSource != null)
            {
                audioSource.Play();
            }

            _routine = StartCoroutine(RunClipRoutine());
        }

        private void Update()
        {
            if (!allowSkip) return;

            if (IsSkipPressed())
            {
                Skip();
            }
        }

        public void Skip()
        {
            if (_transitioning) return;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            StartCoroutine(AdvanceRoutine());
        }

        // Call this from Timeline/Animation event if you want.
        public void OnClipFinished()
        {
            if (_transitioning) return;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            StartCoroutine(AdvanceRoutine());
        }

        private IEnumerator RunClipRoutine()
        {
            if (audioSource != null && audioSource.clip != null)
            {
                // Wait until audio ends.
                while (audioSource.isPlaying)
                {
                    yield return null;
                }

                if (postAudioDelaySeconds > 0f)
                {
                    yield return new WaitForSeconds(postAudioDelaySeconds);
                }
            }
            else if (useFallbackTimerIfNoAudio && fallbackSeconds > 0f)
            {
                yield return new WaitForSeconds(fallbackSeconds);
            }

            yield return AdvanceRoutine();
        }

        private IEnumerator AdvanceRoutine()
        {
            if (_transitioning) yield break;
            _transitioning = true;

            if (fadeCanvasGroup != null && fadeOutSeconds > 0f)
            {
                yield return FadeOut(fadeCanvasGroup, fadeOutSeconds);
            }

            LoadNextScene();
        }

        private static IEnumerator FadeOut(CanvasGroup group, float duration)
        {
            var start = group.alpha;
            var t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }

            group.alpha = 0f;
        }

        private static void LoadNextScene()
        {
            var active = SceneManager.GetActiveScene();
            var nextIndex = active.buildIndex + 1;

            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(0);
                return;
            }

            SceneManager.LoadScene(nextIndex);
        }

        private bool IsSkipPressed()
        {
            // Keyboard / mouse may not exist on some platforms — check for null.
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (skipOnAnyKey && keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            if (skipOnLeftClick && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (keyboard != null && extraSkipKeys != null)
            {
                for (var i = 0; i < extraSkipKeys.Length; i++)
                {
                    var key = keyboard[extraSkipKeys[i]];
                    if (key != null && key.wasPressedThisFrame) return true;
                }
            }

            return false;
        }
    }
}