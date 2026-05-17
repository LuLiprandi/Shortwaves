using System.Collections;
using UnityEngine;

/// <summary>
/// À placer sur le GameObject du lit.
/// Visible uniquement après la séquence anomalie (IsPostAnomaly = true).
/// Interaction : fondu au noir → NextDay() → titre "Jour N" → fondu depuis le noir.
/// </summary>
public class BedInteractable : MonoBehaviour, IInteractable
{
    private const string PromptActive   = "Appuyer sur [E] pour aller dormir";
    private const string PromptDisabled = "";

    [Header("Fondu")]
    [Tooltip("Durée du fondu vers le noir (secondes).")]
    [SerializeField] private float fadeOutDuration = 1.2f;

    [Tooltip("Temps d'attente à l'écran noir avant d'afficher le titre du jour (secondes).")]
    [SerializeField] private float blackHoldDuration = 0.8f;

    [Tooltip("Durée d'affichage du titre 'Jour N' à l'écran noir (secondes).")]
    [SerializeField] private float dayTitleDuration = 2f;

    [Tooltip("Durée du fondu depuis le noir (secondes).")]
    [SerializeField] private float fadeInDuration = 1.2f;

    private bool isTransitioning = false;

    // ── IInteractable ─────────────────────────────────────────────────────────

    /// <summary>Le prompt s'affiche uniquement en post-anomalie.</summary>
    public string PromptMessage =>
        GameStateManager.Instance != null && GameStateManager.Instance.IsPostAnomaly
            ? PromptActive
            : PromptDisabled;

    /// <summary>Lance la séquence de transition jour si les conditions sont réunies.</summary>
    public void Interact()
    {
        if (isTransitioning) return;

        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsPostAnomaly)
            return;

        StartCoroutine(SleepRoutine());
    }

    // ── Séquence ──────────────────────────────────────────────────────────────

    private IEnumerator SleepRoutine()
    {
        isTransitioning = true;

        // Bloquer toute entrée joueur pendant la transition
        GameStateManager.Instance.StartCutscene();

        // Fondu vers le noir
        ScreenFader fader = ScreenFader.Instance;
        if (fader != null)
        {
            bool done = false;
            fader.FadeOut(fadeOutDuration, () => done = true);
            yield return new WaitUntil(() => done);
        }
        else
        {
            yield return new WaitForSeconds(fadeOutDuration);
        }

        // Pause à l'écran noir
        yield return new WaitForSeconds(blackHoldDuration);

        // Avancer au jour suivant
        GameStateManager.Instance.NextDay();

        // Afficher le titre du nouveau jour sur le fond noir
        if (fader != null)
            fader.ShowDayTitle(GameStateManager.Instance.CurrentDay, dayTitleDuration);

        // Attendre que le titre soit affiché avant de faire le fondu de retour
        float totalTitleDuration = dayTitleDuration + 1.2f; // display + fades internes
        yield return new WaitForSeconds(totalTitleDuration);

        // Fondu depuis le noir
        if (fader != null)
        {
            bool done = false;
            fader.FadeIn(fadeInDuration, () => done = true);
            yield return new WaitUntil(() => done);
        }
        else
        {
            yield return new WaitForSeconds(fadeInDuration);
        }

        // Rendre le contrôle au joueur
        GameStateManager.Instance.EndCutscene();

        isTransitioning = false;
    }
}
