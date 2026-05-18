using System.Collections;
using UnityEngine;

namespace Shortwaves
{
    /// <summary>
    /// Orchestre la séquence anomalie du Jour 3 :
    ///   1. Le journal se ferme (géré par JournalManager avant l'appel).
    ///   2. La radio grésille violemment puis s'arrête net.
    ///   3. Silence pesant.
    ///   4. Un message chuchoté et grésillé : « ILS M'ONT TUÉ » sur une fréquence fantôme.
    ///   5. Fondu au noir — le joueur perd le contrôle.
    ///   6. Passage au Jour 4 via GameStateManager.NextDay().
    ///
    /// Appeler TriggerSequence() depuis JournalManager après la validation du décodage.
    /// </summary>
    public class Day3AnomalySequencer : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Day3Data data;

        [Header("Références — systèmes")]
        [Tooltip("RadioSystem piloté pendant la séquence.")]
        [SerializeField] private RadioSystem radioSystem;

        [Tooltip("Séquenceur de fin du Jour 4, déclenché après l'affichage du titre.")]
        [SerializeField] private Day4EndingSequencer day4EndingSequencer;

        [Tooltip("Durée d'affichage du titre 'Jour 4' sur l'écran noir (secondes).")]
        [SerializeField] private float day4TitleDuration = 2.5f;

        [Header("Audio Sources")]
        [Tooltip("AudioSource pour le grésillement violent et le chuchotement.")]
        [SerializeField] private AudioSource anomalySource;

        [Tooltip("SubtitleSystem affiché pendant le chuchotement.")]
        [SerializeField] private SubtitleSystem subtitleSystem;

        // ── État interne ──────────────────────────────────────────────────────

        private bool sequencePlayed;

        private FirstPersonController playerController;
        private InteractionSystem     interactionSystem;

        // Plage de fréquences de la radio (doit correspondre à RadioSystem)
        private const float FrequencyMin = 85f;
        private const float FrequencyMax = 108f;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            playerController  = FindFirstObjectByType<FirstPersonController>();
            interactionSystem = FindFirstObjectByType<InteractionSystem>();
        }

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Déclenche la séquence complète du Jour 3.
        /// Idempotent — ne s'exécute qu'une seule fois par session.
        /// </summary>
        public void TriggerSequence()
        {
            if (sequencePlayed || data == null) return;
            sequencePlayed = true;
            StartCoroutine(AnomalyRoutine());
        }

        // ── Séquence principale ───────────────────────────────────────────────

        private IEnumerator AnomalyRoutine()
        {
            // Verrouiller le joueur pour toute la durée de la séquence
            LockPlayer();

            // Courte pause après la fermeture du journal
            yield return new WaitForSeconds(0.4f);

            // Phase 1 : grésillement violent de la radio
            yield return StartCoroutine(ViolentStaticPhase());

            // Phase 2 : silence absolu
            yield return new WaitForSeconds(data.SilenceDuration);

            // Phase 3 : chuchotement sur fréquence fantôme
            yield return StartCoroutine(WhisperPhase());

            // Délai avant le fondu
            yield return new WaitForSeconds(data.DelayBeforeFade);

            // Verrouiller définitivement la radio — plus d'interaction au Jour 4
            radioSystem?.LockInteraction();

            // Phase 4 : fondu au noir puis passage au jour suivant
            yield return StartCoroutine(FadeAndAdvanceDay());
        }

        // ── Phase 1 : Grésillement violent ───────────────────────────────────

        private IEnumerator ViolentStaticPhase()
        {
            if (anomalySource != null && data.SfxRadioViolentStatic != null)
            {
                anomalySource.loop   = false;
                anomalySource.volume = 1f;
                anomalySource.PlayOneShot(data.SfxRadioViolentStatic);
            }

            // Faire monter puis couper le volume de la radio en parallèle
            if (radioSystem != null)
                radioSystem.SetActive(false);

            float elapsed = 0f;
            while (elapsed < data.ViolentStaticDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Couper le grésillement net
            if (anomalySource != null)
                anomalySource.Stop();
        }

        // ── Phase 2 : Chuchotement fantôme ───────────────────────────────────

        private IEnumerator WhisperPhase()
        {
            if (data.SfxWhisperMessage == null)
            {
                yield return new WaitForSeconds(3f);
                yield break;
            }

            if (anomalySource != null)
            {
                anomalySource.loop   = false;
                anomalySource.volume = 0.85f;
                anomalySource.PlayOneShot(data.SfxWhisperMessage);

                // Lancer les sous-titres synchronisés si disponibles
                if (subtitleSystem != null && data.WhisperSubtitles != null && data.WhisperSubtitles.Length > 0)
                    subtitleSystem.Play(anomalySource, data.WhisperSubtitles);

                yield return new WaitForSeconds(data.SfxWhisperMessage.length);
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }

            // Nettoyer les sous-titres
            subtitleSystem?.Stop();
        }

        // ── Phase 3 : Fondu + passage au jour suivant ─────────────────────────

        private IEnumerator FadeAndAdvanceDay()
        {
            bool fadeComplete = false;
            ScreenFader.Instance?.FadeOut(data.FadeDuration, () => fadeComplete = true);
            yield return new WaitUntil(() => fadeComplete || ScreenFader.Instance == null);

            // Passage au Jour 4
            GameStateManager.Instance?.NextDay();

            // Afficher le titre "Jour 4" sur l'écran noir,
            // puis déclencher BeginDay4() une fois le titre terminé.
            // Day4EndingSequencer gère ensuite le FadeIn et l'ouverture du carnet.
            bool titleDone = false;
            ScreenFader.Instance?.ShowDayTitle(
                GameStateManager.Instance?.CurrentDay ?? 4,
                day4TitleDuration,
                onComplete: () => titleDone = true);

            yield return new WaitUntil(() => titleDone || ScreenFader.Instance == null);

            // Passer le relais — ne pas FadeIn ni UnlockPlayer ici
            Debug.Log($"[Day3] Title done, calling BeginDay4. day4EndingSequencer={day4EndingSequencer}");
            day4EndingSequencer?.BeginDay4();
        }

        // ── Utilitaires ───────────────────────────────────────────────────────

        private void LockPlayer()
        {
            if (playerController  != null) playerController.CanMove  = false;
            if (playerController  != null) playerController.CanLook  = false;
            if (interactionSystem != null) interactionSystem.enabled  = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void UnlockPlayer()
        {
            if (playerController  != null) playerController.CanMove  = true;
            if (playerController  != null) playerController.CanLook  = true;
            if (interactionSystem != null) interactionSystem.enabled  = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}
