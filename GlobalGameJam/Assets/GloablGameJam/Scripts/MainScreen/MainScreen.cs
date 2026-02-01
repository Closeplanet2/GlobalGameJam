using System.Collections;
using CustomLibrary.Scripts.SceneManagement;
using GloablGameJam.Scripts.DreamfireMusicManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GloablGameJam.Scripts.Game;


[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class MainMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Targets")]
    [Tooltip("Optional. If not set, the RectTransform on this GameObject is used.")]
    [SerializeField] private RectTransform target;

    [Tooltip("Optional. If not set, tries to find a Button on this GameObject.")]
    [SerializeField] private Button button;

    [Header("Scene")]
    [SerializeField] private SceneList sceneToLoad = SceneList.MainScreen;

    [Header("Idle Pulse")]
    [Tooltip("Scale at rest (normal size).")]
    [SerializeField] private float normalScale = 1.0f;

    [Tooltip("Scale at pulse peak while idle.")]
    [SerializeField] private float pulsePeakScale = 1.08f;

    [Tooltip("Seconds for a full pulse cycle (up + down).")]
    [SerializeField] private float pulseCycleSeconds = 1.4f;

    [Tooltip("Phase offset (0..1) to desync multiple buttons.")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseOffset = 0f;

    [Header("Hover")]
    [Tooltip("Scale when hovered.")]
    [SerializeField] private float hoverScale = 1.15f;

    [Tooltip("How quickly the button eases to hover/normal when entering/exiting.")]
    [SerializeField] private float hoverEaseSeconds = 0.12f;

    private bool _isHovered;
    private float _phase; // radians progress through the idle sine wave
    private Coroutine _easeRoutine;

    private void Awake()
    {
        if (target == null) target = GetComponent<RectTransform>();
        if (button == null) button = GetComponent<Button>();

        // Convert phaseOffset (0..1) into radians (0..2π)
        _phase = phaseOffset * Mathf.PI * 2f;

        ApplyScale(normalScale);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnEnable()
    {
        if (button != null)
        {
            // Avoid double-binding if re-enabled
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        if (_easeRoutine != null)
        {
            StopCoroutine(_easeRoutine);
            _easeRoutine = null;
        }
    }

    private void Update()
    {
        if (_isHovered) return;

        // Idle pulse: sinusoid between normalScale and pulsePeakScale.
        // sin goes [-1,1] -> remap to [0,1]
        float omega = (Mathf.PI * 2f) / Mathf.Max(0.01f, pulseCycleSeconds);
        _phase += omega * Time.deltaTime;

        float s01 = (Mathf.Sin(_phase) + 1f) * 0.5f;
        float scale = Mathf.Lerp(normalScale, pulsePeakScale, Smooth01(s01));
        ApplyScale(scale);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        EaseToScale(hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;

        // Snap/ease smaller, then idle pulse continues from the current phase value.
        // This means it "carries on playing the animation from there".
        EaseToScale(normalScale);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    private void HandleClick()
    {
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.StartNewRun();
        }  
    }

    private void EaseToScale(float targetScale)
    {
        if (!isActiveAndEnabled) return;

        if (_easeRoutine != null)
        {
            StopCoroutine(_easeRoutine);
            _easeRoutine = null;
        }

        _easeRoutine = StartCoroutine(EaseScaleRoutine(targetScale, hoverEaseSeconds));
    }

    private IEnumerator EaseScaleRoutine(float targetScale, float duration)
    {
        float start = target != null ? target.localScale.x : 1f;
        float elapsed = 0f;

        duration = Mathf.Max(0.001f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Smooth01(elapsed / duration);
            ApplyScale(Mathf.Lerp(start, targetScale, t));
            yield return null;
        }

        ApplyScale(targetScale);
        _easeRoutine = null;
    }

    private void ApplyScale(float scale)
    {
        if (target == null) return;
        target.localScale = new Vector3(scale, scale, 1f);
    }

    private static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t); // SmoothStep
    }
}