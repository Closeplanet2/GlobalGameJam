using TMPro;
using UnityEngine;

namespace DreamfireStudios.UI
{
    /// <summary>
    /// Pulses text color and optional scale for TextMeshPro.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TextPulseTMP : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private TMP_Text target;

        [Header("Color Pulse")]
        [SerializeField] private Color colorA = Color.white;
        [SerializeField] private Color colorB = new Color(1f, 0.85f, 0.25f);
        [SerializeField] private float colorSpeed = 2f;

        [Header("Scale Pulse")]
        [SerializeField] private bool scalePulse = true;
        [SerializeField] private Vector3 scaleA = Vector3.one;
        [SerializeField] private Vector3 scaleB = Vector3.one * 1.05f;
        [SerializeField] private float scaleSpeed = 2.5f;

        private TMP_Text _text;
        private Vector3 _startScale;

        private void Reset()
        {
            target = GetComponent<TMP_Text>();
        }

        private void Awake()
        {
            _text = target;
            _startScale = transform.localScale;
        }

        private void Update()
        {
            if (_text)
            {
                float tColor = (Time.unscaledTime * colorSpeed) % 1f;
                _text.color = Color.Lerp(colorA, colorB, Mathf.Sin(tColor * Mathf.PI * 2f) * 0.5f + 0.5f);
            }

            if (scalePulse)
            {
                float tScale = (Time.unscaledTime * scaleSpeed) % 1f;
                float scaleLerp = Mathf.Sin(tScale * Mathf.PI * 2f) * 0.5f + 0.5f;
                transform.localScale = Vector3.Lerp(scaleA, scaleB, scaleLerp);
            }
        }

        private void OnDisable()
        {
            transform.localScale = _startScale;
        }
    }
}