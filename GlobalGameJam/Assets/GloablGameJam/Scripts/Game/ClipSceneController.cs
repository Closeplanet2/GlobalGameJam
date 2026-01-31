using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GloablGameJam.Scripts.Game
{
    /// <summary>
    /// Simple "clip" scene controller: plays an audio clip over a static image (optionally animated),
    /// then advances to the next scene (usually a gameplay level).
    /// </summary>
    public sealed class ClipSceneController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool playOnStart = true;

        [Header("Timing")]
        [Tooltip("Extra time after audio finishes before advancing.")]
        [SerializeField, Min(0f)] private float postAudioDelaySeconds = 0.25f;

        [Tooltip("If true and audioSource is missing/has no clip, auto-advance after fallbackSeconds.")]
        [SerializeField] private bool useFallbackTimerIfNoAudio = true;

        [SerializeField, Min(0f)] private float fallbackSeconds = 3.0f;

        [Header("Skip")]
        [SerializeField] private bool allowSkip = true;
        [Tooltip("Any key or left mouse click will skip.")]
        [SerializeField] private bool skipOnAnyInput = true;

        [Header("Optional Fade")]
        [Tooltip("If set, fades this canvas group before scene advance.")]
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

            if (skipOnAnyInput)
            {
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                {
                    Skip();
                }
            }
        }

        public void Skip()
        {
            if (_transitioning) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(AdvanceRoutine());
        }

        private IEnumerator RunClipRoutine()
        {
            // Wait for audio to finish, or fallback timer.
            if (audioSource != null && audioSource.clip != null)
            {
                // If audio is not playing yet, wait until it starts or ends.
                // Then wait until it stops.
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

            // Fade out (optional)
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
                t += Time.deltaTime;
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
                // Safety fallback: go to scene 0.
                SceneManager.LoadScene(0);
                return;
            }

            SceneManager.LoadScene(nextIndex);
        }
    }
}