using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Image fadeImage;

    [Header("Settings")]
    [SerializeField] private float defaultFadeDuration = 1f;

    private const float OPAQUE = 1f;
    private const float TRANSPARENT = 0f;

    private void Start()
    {
        FadeIn();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (fadeImage != null)
            fadeImage.color = new Color(0f, 0f, 0f, OPAQUE);
    }

    public void FadeIn(Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(OPAQUE, TRANSPARENT, defaultFadeDuration, onComplete));
    }

    public void FadeOut(Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(TRANSPARENT, OPAQUE, defaultFadeDuration, onComplete));
    }

    public void FadeOutAndIn(float holdDuration = 1f, Action onHoldComplete = null, Action onComplete = null)
    {
        StartCoroutine(FadeOutAndInRoutine(holdDuration, onHoldComplete, onComplete));
    }

    private IEnumerator FadeOutAndInRoutine(float holdDuration, Action onHoldComplete, Action onComplete)
    {
        yield return FadeRoutine(TRANSPARENT, OPAQUE, defaultFadeDuration, null);
        onHoldComplete?.Invoke();
        yield return new WaitForSeconds(holdDuration);
        yield return FadeRoutine(OPAQUE, TRANSPARENT, defaultFadeDuration, null);
        onComplete?.Invoke();
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = to;
        fadeImage.color = color;
        onComplete?.Invoke();
    }
}
