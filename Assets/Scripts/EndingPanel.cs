using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI singleton — panneau plein écran pour les cinématiques de fin de jeu.
/// Gère :
///   - Un fondu depuis blanc ou noir vers une image de fin statique.
///   - Un son de fond joué en parallèle.
///   - Un fondu final au noir avant le générique.
/// Construit dynamiquement comme ScreenFader (pas de Prefab requis).
/// </summary>
public class EndingPanel : MonoBehaviour
{
    public static EndingPanel Instance { get; private set; }

    // ── Composants UI ─────────────────────────────────────────────────────────

    private Canvas        canvas;
    private CanvasGroup   backgroundGroup;  // overlay de couleur (noir ou blanc)
    private Image         backgroundImage;
    private Image         endingImage;
    private CanvasGroup   endingImageGroup;
    private AudioSource   audioSource;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Lance la séquence complète d'une fin :
    ///   1. Fondu depuis <paramref name="startColor"/> vers transparent (révèle <paramref name="endSprite"/>).
    ///   2. Maintien de l'image.
    ///   3. Fondu vers noir.
    ///   4. Invoque <paramref name="onComplete"/>.
    /// Le son <paramref name="sfx"/> est joué dès le début et se coupe progressivement à la fin.
    /// </summary>
    public void PlayEnding(
        Sprite    endSprite,
        AudioClip sfx,
        Color     startColor,
        float     fadeInDuration,
        float     holdDuration,
        float     fadeOutDuration,
        Action    onComplete = null)
    {
        StartCoroutine(EndingRoutine(endSprite, sfx, startColor,
            fadeInDuration, holdDuration, fadeOutDuration, onComplete));
    }

    /// <summary>Masque le panneau immédiatement sans animation.</summary>
    public void Hide()
    {
        backgroundGroup.alpha = 0f;
        backgroundGroup.blocksRaycasts = false;
        endingImageGroup.alpha = 0f;
        if (audioSource.isPlaying) audioSource.Stop();
    }

    // ── Coroutine principale ──────────────────────────────────────────────────

    private IEnumerator EndingRoutine(
        Sprite    endSprite,
        AudioClip sfx,
        Color     startColor,
        float     fadeInDuration,
        float     holdDuration,
        float     fadeOutDuration,
        Action    onComplete)
    {
        // Préparer l'image de fin (invisible)
        endingImage.sprite     = endSprite;
        endingImage.preserveAspect = true;
        endingImageGroup.alpha = 0f;

        // Positionner la couleur de départ plein écran et visible
        backgroundImage.color      = startColor;
        backgroundGroup.alpha      = 1f;
        backgroundGroup.blocksRaycasts = true;

        // Lancer le son
        if (sfx != null)
        {
            audioSource.clip   = sfx;
            audioSource.loop   = true;
            audioSource.volume = 1f;
            audioSource.Play();
        }

        // Fondu de l'image de fin (alpha 0 → 1) pendant que la couleur de fond fond (alpha 1 → 0)
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            endingImageGroup.alpha = t;
            backgroundGroup.alpha  = 1f - t;
            yield return null;
        }
        endingImageGroup.alpha = 1f;
        backgroundGroup.alpha  = 0f;

        // Maintien de l'image
        yield return new WaitForSeconds(holdDuration);

        // Fondu au noir (image + fond → noir)
        backgroundImage.color  = Color.black;
        backgroundGroup.alpha  = 0f;

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            endingImageGroup.alpha = 1f - t;
            backgroundGroup.alpha  = t;

            // Baisser progressivement le volume audio
            if (audioSource.isPlaying)
                audioSource.volume = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }
        endingImageGroup.alpha = 0f;
        backgroundGroup.alpha  = 1f;
        if (audioSource.isPlaying) audioSource.Stop();

        onComplete?.Invoke();
    }

    // ── Construction UI ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        canvas             = gameObject.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998; // sous ScreenFader (999) mais au-dessus de tout le reste

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();

        // Image de fin — plein écran, invisible par défaut
        var endingGO = new GameObject("EndingImage");
        endingGO.transform.SetParent(transform, false);

        var endingRT       = endingGO.AddComponent<RectTransform>();
        endingRT.anchorMin = Vector2.zero;
        endingRT.anchorMax = Vector2.one;
        endingRT.offsetMin = endingRT.offsetMax = Vector2.zero;

        endingImage = endingGO.AddComponent<Image>();
        endingImage.raycastTarget = false;

        endingImageGroup               = endingGO.AddComponent<CanvasGroup>();
        endingImageGroup.alpha         = 0f;
        endingImageGroup.interactable  = false;
        endingImageGroup.blocksRaycasts = false;

        // Overlay de couleur (blanc ou noir) pour le fondu de départ et d'arrivée
        var bgGO = new GameObject("ColorOverlay");
        bgGO.transform.SetParent(transform, false);

        var bgRT       = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        backgroundImage               = bgGO.AddComponent<Image>();
        backgroundImage.color         = Color.black;
        backgroundImage.raycastTarget = true;

        backgroundGroup               = bgGO.AddComponent<CanvasGroup>();
        backgroundGroup.alpha         = 0f;
        backgroundGroup.interactable  = false;
        backgroundGroup.blocksRaycasts = false;

        // AudioSource pour la musique/ambiance de fin
        audioSource        = gameObject.AddComponent<AudioSource>();
        audioSource.loop   = false;
        audioSource.volume = 1f;
        audioSource.playOnAwake = false;
    }
}
