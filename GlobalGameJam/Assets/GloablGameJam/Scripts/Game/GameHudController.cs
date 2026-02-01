using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GloablGameJam.Scripts.Game
{
    /// <summary>
    /// HUD controller:
    /// - Hearts (persist across run)
    /// - Keys (reset per level)
    /// - Run timer (paused during clip scenes)
    ///
    /// Robustness:
    /// - Force refresh on enable, on scene load, and next frame (covers spawn order issues).
    /// - Works with UiPopIcon if present; otherwise falls back to Image.enabled.
    /// - Timer text pops (scale) once per second while not paused.
    /// </summary>
    public sealed class GameHudController : MonoBehaviour
    {
        [Header("Icons")]
        [SerializeField] private Image[] heartIcons;
        [SerializeField] private Image[] keyIcons;

        [Header("Run Timer Text")]
        [SerializeField] private TMP_Text runTimerText;

        [Header("Timer Formatting")]
        [SerializeField] private bool showMilliseconds = true;

        [Header("Timer Pop")]
        [SerializeField] private bool popOnSecondChange = true;
        [SerializeField, Min(0.01f)] private float timerPopDuration = 0.10f;
        [SerializeField, Min(1f)] private float timerPopScale = 1.15f;

        [Header("Paused Look")]
        [SerializeField] private bool dimWhenPaused = true;
        [SerializeField, Range(0f, 1f)] private float pausedAlpha = 0.6f;

        private GameSessionManager _session;
        private int _lastWholeSecond = -1;

        private Coroutine _timerPopRoutine;
        private Vector3 _timerBaseScale = Vector3.one;

        private void Awake()
        {
            if (runTimerText != null)
            {
                _timerBaseScale = runTimerText.rectTransform.localScale;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            HookSession();
            ForceRefreshNow();
            StartCoroutine(ForceRefreshNextFrame());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            UnhookSession();

            if (_timerPopRoutine != null)
            {
                StopCoroutine(_timerPopRoutine);
                _timerPopRoutine = null;
            }

            if (runTimerText != null)
            {
                runTimerText.rectTransform.localScale = _timerBaseScale;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Scene load is a common point where HUDs get out of sync.
            HookSession();
            ForceRefreshNow();
            StartCoroutine(ForceRefreshNextFrame());
        }

        private void HookSession()
        {
            var session = GameSessionManager.Instance;
            if (session == null || session == _session) return;

            UnhookSession();

            _session = session;
            _session.HeartsChanged += OnHeartsChanged;
            _session.KeysChanged += OnKeysChanged;
            _session.RunTimeUpdated += OnRunTimeUpdated;
            _session.RunTimerPausedChanged += OnRunTimerPausedChanged;
        }

        private void UnhookSession()
        {
            if (_session == null) return;

            _session.HeartsChanged -= OnHeartsChanged;
            _session.KeysChanged -= OnKeysChanged;
            _session.RunTimeUpdated -= OnRunTimeUpdated;
            _session.RunTimerPausedChanged -= OnRunTimerPausedChanged;

            _session = null;
        }

        private IEnumerator ForceRefreshNextFrame()
        {
            // Waiting one frame ensures:
            // - GameSessionManager has applied sceneLoaded resets
            // - HUD references are active
            // - avoids “UI didn’t reset” from spawn order
            yield return null;
            ForceRefreshNow();
        }

        private void ForceRefreshNow()
        {
            HookSession();

            if (_session == null)
            {
                // If session isn't available yet, show a safe default.
                PaintHearts(0);
                PaintKeys(0);
                PaintTimer(0f);
                SetTimerPausedLook(false);
                return;
            }

            PaintHearts(_session.HeartsRemaining);
            PaintKeys(_session.KeysUnlocked);
            PaintTimer(_session.RunTimeSeconds);
            SetTimerPausedLook(_session.IsRunTimerPaused);
        }

        private void OnHeartsChanged(int heartsRemaining) => PaintHearts(heartsRemaining);

        private void OnKeysChanged(int keysUnlocked, int keysRequired) => PaintKeys(keysUnlocked);

        private void PaintHearts(int heartsRemaining)
        {
            if (heartIcons == null) return;

            for (var i = 0; i < heartIcons.Length; i++)
            {
                var img = heartIcons[i];
                if (img == null) continue;

                var shouldBeOn = i < heartsRemaining;
                SetIconState(img, shouldBeOn);
            }
        }

        private void PaintKeys(int keysUnlocked)
        {
            if (keyIcons == null) return;

            for (var i = 0; i < keyIcons.Length; i++)
            {
                var img = keyIcons[i];
                if (img == null) continue;

                var shouldBeOn = i < keysUnlocked;
                SetIconState(img, shouldBeOn);
            }
        }

        private void OnRunTimeUpdated(float seconds)
        {
            PaintTimer(seconds);

            if (!popOnSecondChange) return;
            if (_session != null && _session.IsRunTimerPaused) return;

            var whole = Mathf.FloorToInt(seconds);
            if (whole != _lastWholeSecond)
            {
                _lastWholeSecond = whole;
                TriggerTimerPop();
            }
        }

        private void PaintTimer(float seconds)
        {
            if (runTimerText == null) return;

            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = Mathf.FloorToInt(seconds % 60f);

            if (!showMilliseconds)
            {
                runTimerText.text = $"{minutes:00}:{secs:00}";
            }
            else
            {
                var millis = Mathf.FloorToInt((seconds * 1000f) % 1000f);
                runTimerText.text = $"{minutes:00}:{secs:00}.{millis:000}";
            }
        }

        private void OnRunTimerPausedChanged(bool paused)
        {
            SetTimerPausedLook(paused);
        }

        private void SetTimerPausedLook(bool paused)
        {
            if (!dimWhenPaused || runTimerText == null) return;

            var c = runTimerText.color;
            c.a = paused ? pausedAlpha : 1f;
            runTimerText.color = c;
        }

        private void TriggerTimerPop()
        {
            if (runTimerText == null) return;

            if (_timerPopRoutine != null)
            {
                StopCoroutine(_timerPopRoutine);
                _timerPopRoutine = null;
            }

            _timerPopRoutine = StartCoroutine(TimerPopRoutine());
        }

        private IEnumerator TimerPopRoutine()
        {
            var rt = runTimerText.rectTransform;
            rt.localScale = _timerBaseScale;

            // Up
            var t = 0f;
            while (t < timerPopDuration)
            {
                t += Time.unscaledDeltaTime;
                var a = Mathf.Clamp01(t / timerPopDuration);
                a = a * a * (3f - 2f * a); // smoothstep

                rt.localScale = Vector3.LerpUnclamped(_timerBaseScale, _timerBaseScale * timerPopScale, a);
                yield return null;
            }

            // Down
            t = 0f;
            var downDur = timerPopDuration * 0.75f;
            var start = rt.localScale;

            while (t < downDur)
            {
                t += Time.unscaledDeltaTime;
                var a = Mathf.Clamp01(t / downDur);
                a = a * a * (3f - 2f * a);

                rt.localScale = Vector3.LerpUnclamped(start, _timerBaseScale, a);
                yield return null;
            }

            rt.localScale = _timerBaseScale;
            _timerPopRoutine = null;
        }

        private static void SetIconState(Image img, bool visible)
        {
            // Prefer pop animation if available.
            var pop = img.GetComponent<UiPopIcon>();
            if (pop != null)
            {
                if (visible && !img.enabled) pop.Show();
                else if (!visible && img.enabled) pop.Hide();
                return;
            }

            img.enabled = visible;
        }
    }
}