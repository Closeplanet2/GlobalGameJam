using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GloablGameJam.Scripts.Game
{
    /// <summary>
    /// Pops a UI icon in/out when toggled. Works with unscaled time so it feels consistent.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Graphic))]
    public sealed class UiPopIcon : MonoBehaviour
    {
        [Header("Pop In")]
        [SerializeField, Min(0.01f)] private float popInDuration = 0.12f;
        [SerializeField, Min(1f)] private float popInOvershootScale = 1.15f;

        [Header("Pop Out")]
        [SerializeField, Min(0.01f)] private float popOutDuration = 0.10f;

        private RectTransform _rt;
        private Graphic _graphic;
        private Coroutine _routine;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _graphic = GetComponent<Graphic>();
        }

        /// <summary>
        /// Shows the icon and plays a pop-in animation.
        /// </summary>
        public void Show()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine());
        }

        /// <summary>
        /// Plays a pop-out animation, then hides the icon.
        /// </summary>
        public void Hide()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(HideRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            // Ensure visible and reset.
            _graphic.enabled = true;
            _rt.localScale = Vector3.zero;

            // Phase 1: 0 -> overshoot
            yield return ScaleTo(Vector3.one * popInOvershootScale, popInDuration * 0.65f);

            // Phase 2: overshoot -> 1
            yield return ScaleTo(Vector3.one, popInDuration * 0.35f);

            _routine = null;
        }

        private IEnumerator HideRoutine()
        {
            // If already hidden, do nothing.
            if (!_graphic.enabled)
            {
                _routine = null;
                yield break;
            }

            // Scale down, then disable.
            yield return ScaleTo(Vector3.zero, popOutDuration);

            _graphic.enabled = false;
            _rt.localScale = Vector3.one; // keep layout stable if re-enabled

            _routine = null;
        }

        private IEnumerator ScaleTo(Vector3 target, float duration)
        {
            duration = Mathf.Max(0.01f, duration);

            var start = _rt.localScale;
            var t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var a = Mathf.Clamp01(t / duration);

                // Smoothstep easing.
                a = a * a * (3f - 2f * a);

                _rt.localScale = Vector3.LerpUnclamped(start, target, a);
                yield return null;
            }

            _rt.localScale = target;
        }
    }
}