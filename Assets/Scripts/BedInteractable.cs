using System.Collections;
using UnityEngine;

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

    public string PromptMessage =>
        GameStateManager.Instance != null && GameStateManager.Instance.IsPostAnomaly
            ? PromptActive
            : PromptDisabled;

    public void Interact()
    {
        if (isTransitioning) return;

        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsPostAnomaly)
            return;

        StartCoroutine(SleepRoutine());
    }

    private IEnumerator SleepRoutine()
    {
        isTransitioning = true;

        GameStateManager.Instance.StartCutscene();

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

        yield return new WaitForSeconds(blackHoldDuration);

        GameStateManager.Instance.NextDay();

        if (fader != null)
            fader.ShowDayTitle(GameStateManager.Instance.CurrentDay, dayTitleDuration);

        float totalTitleDuration = dayTitleDuration + 1.2f;
        yield return new WaitForSeconds(totalTitleDuration);

        if (GameStateManager.Instance.CurrentDay == 4)
        {
            GameStateManager.Instance.EndCutscene();
            isTransitioning = false;
            yield break;
        }

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

        GameStateManager.Instance.EndCutscene();

        isTransitioning = false;
    }
}
