using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton — superpose un panneau noir sur l'écran pour les fondus.
/// Usage : ScreenFader.Instance.FadeOut(duration, onComplete) / FadeIn(duration, onComplete)
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Tooltip("Durée par défaut d'un fondu (secondes).")]
    [SerializeField] private float defaultDuration = 1f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();
        canvasGroup.alpha = 0f;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Fondu vers le noir (alpha 0 → 1).</summary>
    public void FadeOut(float duration = -1f, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, 1f, duration < 0f ? defaultDuration : duration, onComplete));
    }

    /// <summary>Fondu depuis le noir (alpha 1 → 0).</summary>
    public void FadeIn(float duration = -1f, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, 0f, duration < 0f ? defaultDuration : duration, onComplete));
    }

    /// <summary>Force l'écran au noir instantanément sans animation.</summary>
    public void SetBlack() => canvasGroup.alpha = 1f;

    /// <summary>Force l'écran transparent instantanément sans animation.</summary>
    public void SetClear() => canvasGroup.alpha = 0f;

    // ── Interne ───────────────────────────────────────────────────────────────

    private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
        canvasGroup.blocksRaycasts = to > 0.5f;

        onComplete?.Invoke();
    }

    private void BuildOverlay()
    {
        // Canvas persistant au-dessus de tout
        var canvas          = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        gameObject.AddComponent<GraphicRaycaster>();

        // Panneau noir plein écran
        var panelGO = new GameObject("FadePanel");
        panelGO.transform.SetParent(transform, false);

        var rt          = panelGO.AddComponent<RectTransform>();
        rt.anchorMin    = Vector2.zero;
        rt.anchorMax    = Vector2.one;
        rt.offsetMin    = rt.offsetMax = Vector2.zero;

        var img         = panelGO.AddComponent<Image>();
        img.color       = Color.black;
        img.raycastTarget = true;

        canvasGroup     = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable    = false;
        canvasGroup.blocksRaycasts  = false;
    }
}
