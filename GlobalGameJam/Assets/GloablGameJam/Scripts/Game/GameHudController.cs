using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GloablGameJam.Scripts.Game
{
    /// <summary>
    /// HUD controller that displays:
    /// - Hearts (persist across scenes within a run)
    /// - Keys (reset per level, increase when collected)
    /// - Global run timer text
    ///
    /// Setup:
    /// - Assign heartIcons (3 images)
    /// - Assign keyIcons (3 images)
    /// - Assign runTimerText (TMP)
    ///
    /// Optional:
    /// - If each icon has UiPopIcon, it will pop on/off. Otherwise it toggles Image.enabled.
    /// </summary>
    public sealed class GameHudController : MonoBehaviour
    {
        [Header("Icons")]
        [SerializeField] private Image[] heartIcons;
        [SerializeField] private Image[] keyIcons;

        [Header("Text")]
        [SerializeField] private TMP_Text runTimerText;

        [Header("Timer Formatting")]
        [Tooltip("If true, shows milliseconds as MM:SS.mmm, else MM:SS.")]
        [SerializeField] private bool showMilliseconds = true;

        private GameSessionManager _session;

        private void OnEnable()
        {
            _session = GameSessionManager.Instance;
            if (_session == null) return;

            _session.HeartsChanged += OnHeartsChanged;
            _session.KeysChanged += OnKeysChanged;
            _session.RunTimeUpdated += OnRunTimeUpdated;

            // Initial paint
            OnHeartsChanged(_session.HeartsRemaining);
            OnKeysChanged(_session.KeysUnlocked, _session.KeysRequired);
            OnRunTimeUpdated(_session.RunTimeSeconds);
        }

        private void OnDisable()
        {
            if (_session == null) return;

            _session.HeartsChanged -= OnHeartsChanged;
            _session.KeysChanged -= OnKeysChanged;
            _session.RunTimeUpdated -= OnRunTimeUpdated;

            _session = null;
        }

        private void OnHeartsChanged(int heartsRemaining)
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

        private void OnKeysChanged(int keysUnlocked, int keysRequired)
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
            if (runTimerText == null) return;

            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = Mathf.FloorToInt(seconds % 60f);

            if (!showMilliseconds)
            {
                runTimerText.text = $"{minutes:00}:{secs:00}";
                return;
            }

            var millis = Mathf.FloorToInt((seconds * 1000f) % 1000f);
            runTimerText.text = $"{minutes:00}:{secs:00}.{millis:000}";
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