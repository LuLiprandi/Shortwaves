using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton UI — affiche l'image de fin plein écran avec un fondu d'entrée.
/// Le joueur clique n'importe où pour revenir au menu principal.
/// Construit dynamiquement, aucun Prefab requis.
/// </summary>
public class EndingPanel : MonoBehaviour
{
    public static EndingPanel Instance { get; private set; }

    // ── Composants UI ─────────────────────────────────────────────────────────

    private Canvas      canvas;
    private Image       endingImage;
    private CanvasGroup endingGroup;
    private AudioSource audioSource;

    // ── Etat ──────────────────────────────────────────────────────────────────

    private bool        clickable;
    private Action      onClickCallback;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void Update()
    {
        if (!clickable) return;
        if (Input.anyKeyDown)
        {
            clickable = false;
            onClickCallback?.Invoke();
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Affiche l'image de fin en fondu depuis le noir.
    /// Le joueur clique pour déclencher <paramref name="onClicked"/>.
    /// </summary>
    public void ShowImage(Texture2D texture, AudioClip sfx, float fadeInDuration, Action onClicked)
    {
        if (texture == null)
        {
            onClicked?.Invoke();
            return;
        }

        onClickCallback = onClicked;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        endingImage.sprite = sprite;
        endingImage.preserveAspect = true;

        endingGroup.alpha           = 0f;
        endingGroup.blocksRaycasts  = true;
        endingGroup.interactable    = false;
        canvas.enabled              = true;

        if (sfx != null)
        {
            audioSource.clip   = sfx;
            audioSource.loop   = true;
            audioSource.volume = 1f;
            audioSource.Play();
        }

        StartCoroutine(FadeInRoutine(fadeInDuration));
    }

    /// <summary>Masque le panneau immédiatement.</summary>
    public void Hide()
    {
        clickable              = false;
        canvas.enabled         = false;
        endingGroup.alpha      = 0f;
        if (audioSource.isPlaying) audioSource.Stop();
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator FadeInRoutine(float duration)
    {
        // Délai minimal pour éviter un clic accidentel au moment de l'affichage
        yield return new WaitForSeconds(0.3f);
        clickable = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed           += Time.deltaTime;
            endingGroup.alpha  = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        endingGroup.alpha = 1f;
    }

    // ── Construction UI ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        canvas            = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvas.enabled    = false;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();

        // Fond noir plein écran
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(transform, false);
        var bgRT       = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg      = bgGO.AddComponent<Image>();
        bgImg.color    = Color.black;
        bgImg.raycastTarget = true;

        // Image de fin plein écran
        var imgGO = new GameObject("EndingImage");
        imgGO.transform.SetParent(transform, false);
        var imgRT       = imgGO.AddComponent<RectTransform>();
        imgRT.anchorMin = Vector2.zero;
        imgRT.anchorMax = Vector2.one;
        imgRT.offsetMin = imgRT.offsetMax = Vector2.zero;
        endingImage     = imgGO.AddComponent<Image>();
        endingImage.raycastTarget = false;

        endingGroup                  = imgGO.AddComponent<CanvasGroup>();
        endingGroup.alpha            = 0f;
        endingGroup.interactable     = false;
        endingGroup.blocksRaycasts   = false;

        // Audio
        audioSource            = gameObject.AddComponent<AudioSource>();
        audioSource.loop       = true;
        audioSource.volume     = 1f;
        audioSource.playOnAwake = false;
    }
}
