using System.Collections;
using CustomLibrary.Scripts.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class SplashScreenAnimator : MonoBehaviour
{
    [Header("Bloom")]
    [SerializeField] private Volume volume;
    [SerializeField] private float bloomPeakIntensity = 12f;
    [SerializeField] private float bloomNormalIntensity = 2f;
    [SerializeField] private float bloomPulseDuration = 0.4f;
    [SerializeField] private int bloomPulses = 3;

    [SerializeField] private float delayBeforeAnimation= 0.5f;
    [SerializeField] private float delayBeforeSceneLoad = 0.5f;

    private Bloom _bloom;

    private void Awake()
    {
        if (volume == null || !volume.profile.TryGet(out _bloom))
        {
            Debug.LogError("[SplashScreenAnimator] Bloom not found on Volume.");
            enabled = false;
            return;
        }

        _bloom.intensity.Override(bloomNormalIntensity);
    }

    private void Start()
    {
        StartCoroutine(PlaySplashSequence());
    }

    private IEnumerator PlaySplashSequence()
    {
        yield return new WaitForSeconds(delayBeforeAnimation);
        for (int i = 0; i < bloomPulses; i++)
        {
            yield return PulseBloom();
        }
        yield return new WaitForSeconds(delayBeforeSceneLoad);
        SceneManagement.Instance.LoadLevel(SceneList.MainScreen.ToString());
    }

    private IEnumerator PulseBloom()
    {
        yield return AnimateBloom(bloomNormalIntensity, bloomPeakIntensity);
        yield return AnimateBloom(bloomPeakIntensity, bloomNormalIntensity);
    }

    private IEnumerator AnimateBloom(float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < bloomPulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / bloomPulseDuration);
            _bloom.intensity.Override(Mathf.Lerp(from, to, t));
            yield return null;
        }

        _bloom.intensity.Override(to);
    }
}