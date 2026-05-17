using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton — superpose un panneau noir sur l'écran pour les fondus.
/// Démarre automatiquement en noir, affiche le titre du jour courant, puis fait un fondu de retour.
/// Usage : ScreenFader.Instance.FadeOut(duration, onComplete) / FadeIn(duration, onComplete)
/// Pour les transitions de jour : ScreenFader.Instance.ShowDayTitle(day, displayDuration)
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Tooltip("Durée par défaut d'un fondu (secondes).")]
    [SerializeField] private float defaultDuration = 1f;

    [Header("Titre de jour — démarrage")]
    [Tooltip("Temps de noir pur avant l'apparition du titre au démarrage (secondes).")]
    [SerializeField] private float initialBlackHold = 1.5f;

    [Tooltip("Durée d'affichage du titre 'Jour N' au démarrage (secondes).")]
    [SerializeField] private float initialTitleDuration = 2.5f;

    [Tooltip("Durée du fondu depuis le noir après le titre de démarrage (secondes).")]
    [SerializeField] private float initialFadeInDuration = 1.5f;

    [Tooltip("Police utilisée pour le titre de jour. Si vide, utilise la police par défaut TMP.")]
    [SerializeField] private TMP_FontAsset dayTitleFont;

    private CanvasGroup canvasGroup;
    private TextMeshProUGUI dayTitleText;

    // Coroutines stockées séparément pour éviter StopAllCoroutines()
    private Coroutine fadeCoroutine;
    private Coroutine titleCoroutine;
    private Coroutine startupCoroutine;

    private const float DayTitleFadeDuration = 0.6f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();

        // Démarrer en noir pour l'intro
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    private void Start()
    {
        startupCoroutine = StartCoroutine(StartupSequence());
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Fondu vers le noir (alpha 0 → 1).</summary>
    public void FadeOut(float duration = -1f, Action onComplete = null)
    {
        StopActiveCoroutines();
        fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, duration < 0f ? defaultDuration : duration, onComplete));
    }

    /// <summary>Fondu depuis le noir (alpha 1 → 0).</summary>
    public void FadeIn(float duration = -1f, Action onComplete = null)
    {
        StopActiveCoroutines();
        fadeCoroutine = StartCoroutine(FadeRoutine(1f, 0f, duration < 0f ? defaultDuration : duration, onComplete));
    }

    /// <summary>Force l'écran au noir instantanément sans animation.</summary>
    public void SetBlack()
    {
        StopActiveCoroutines();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    /// <summary>Force l'écran transparent instantanément sans animation.</summary>
    public void SetClear()
    {
        StopActiveCoroutines();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Affiche "Jour N" centré sur le fond noir pendant <paramref name="displayDuration"/> secondes.
    /// Le texte apparaît et disparaît en fondu. Doit être appelé pendant que l'écran est déjà noir.
    /// </summary>
    public void ShowDayTitle(int day, float displayDuration = 2f)
    {
        if (titleCoroutine != null) StopCoroutine(titleCoroutine);
        titleCoroutine = StartCoroutine(DayTitleRoutine(day, displayDuration));
    }

    // ── Interne ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Arrête les coroutines de fondu et de startup sans toucher à la coroutine de titre.
    /// </summary>
    private void StopActiveCoroutines()
    {
        if (startupCoroutine != null)
        {
            StopCoroutine(startupCoroutine);
            startupCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    /// <summary>
    /// Séquence automatique au démarrage : noir → pause → titre du jour → fondu de retour.
    /// </summary>
    private IEnumerator StartupSequence()
    {
        // Attendre un frame que GameStateManager soit initialisé
        yield return null;

        int day = GameStateManager.Instance != null ? GameStateManager.Instance.CurrentDay : 1;

        // Pause noire initiale
        yield return new WaitForSeconds(initialBlackHold);

        // Titre du jour (inline pour ne pas créer de sous-coroutine killable séparément)
        yield return StartCoroutine(DayTitleRoutine(day, initialTitleDuration));

        // Fondu de retour vers la scène
        yield return StartCoroutine(FadeRoutine(1f, 0f, initialFadeInDuration, null));

        startupCoroutine = null;
    }

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

    private IEnumerator DayTitleRoutine(int day, float displayDuration)
    {
        dayTitleText.text = $"Jour {day}";

        // Fade in texte
        float elapsed = 0f;
        Color c = dayTitleText.color;
        while (elapsed < DayTitleFadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / DayTitleFadeDuration);
            dayTitleText.color = c;
            yield return null;
        }
        c.a = 1f;
        dayTitleText.color = c;

        // Maintien
        yield return new WaitForSeconds(displayDuration);

        // Fade out texte
        elapsed = 0f;
        while (elapsed < DayTitleFadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / DayTitleFadeDuration);
            dayTitleText.color = c;
            yield return null;
        }
        c.a = 0f;
        dayTitleText.color = c;
    }

    private void BuildOverlay()
    {
        // Canvas persistant au-dessus de tout
        var canvas          = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();

        // Panneau noir plein écran
        var panelGO = new GameObject("FadePanel");
        panelGO.transform.SetParent(transform, false);

        var rt       = panelGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img           = panelGO.AddComponent<Image>();
        img.color         = Color.black;
        img.raycastTarget = true;

        canvasGroup                = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

        // Texte "Jour N" — centré, blanc transparent par défaut
        var titleGO = new GameObject("DayTitle");
        titleGO.transform.SetParent(panelGO.transform, false);

        var titleRT       = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.3f, 0.4f);
        titleRT.anchorMax = new Vector2(0.7f, 0.6f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        dayTitleText               = titleGO.AddComponent<TextMeshProUGUI>();
        dayTitleText.text          = string.Empty;
        dayTitleText.fontSize      = 52f;
        dayTitleText.fontStyle     = FontStyles.Italic;
        dayTitleText.alignment     = TextAlignmentOptions.Center;
        dayTitleText.color         = new Color(1f, 1f, 1f, 0f);
        dayTitleText.raycastTarget = false;

        if (dayTitleFont != null)
            dayTitleText.font = dayTitleFont;
    }
}
