using System.Collections;
using UnityEngine;

namespace Shortwaves
{
    /// <summary>
    /// Orchestre le Jour 4 — dernier jour, deux fins selon le choix de la porte au Jour 2.
    ///
    /// Flux commun (après BedInteractable.SleepRoutine, écran DÉJÀ noir + titre "Jour 4" affiché) :
    ///   1. Le carnet s'ouvre automatiquement sur fond noir — les pensées apparaissent sur le noir.
    ///   2. Le joueur lit, puis ferme le carnet (touche ESC / J).
    ///   3. Fondu depuis le noir — la scène réapparaît.
    ///   4. Branching Fin A / Fin B.
    ///
    /// Fin A (porte OUVERTE au J2) :
    ///   - Joueur libre, seule la porte métallique est interactive.
    ///   - Interaction porte → fondu au blanc (blizzard) → image de fin → générique.
    ///
    /// Fin B (porte IGNORÉE au J2) :
    ///   - Joueur bloqué sur sa chaise.
    ///   - Alarme industrielle → fondu au blanc → fondu au noir brutal → image de fin → générique.
    ///
    /// Appeler BeginDay4() depuis JournalManager.HandleDayChanged(4) ou au Start si déjà Jour 4.
    /// </summary>
    public class Day4EndingSequencer : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Day4Data data;

        [Header("Fin A — door interaction")]
        [Tooltip("Interactable placé sur la manivelle / porte métallique (Fin A uniquement).")]
        [SerializeField] private FinalDoorInteractable finalDoor;

        [Header("Fin B — audio")]
        [Tooltip("AudioSource dédiée aux sons séquentiels de Fin B (alarme, pas, voix).")]
        [SerializeField] private AudioSource finBSource;

        // ── État interne ──────────────────────────────────────────────────────

        private bool day4Started;
        private Day2DoorChoice endingVariant;

        private FirstPersonController playerController;
        private InteractionSystem     interactionSystem;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            playerController  = FindFirstObjectByType<FirstPersonController>();
            interactionSystem = FindFirstObjectByType<InteractionSystem>();
        }

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Démarre la séquence du Jour 4.
        /// Appelé par Day3AnomalySequencer (skipFadeIn = false) ou par JournalManager
        /// depuis un slot de test Jour 4 (skipFadeIn = true, la scène est déjà visible).
        /// Idempotent — ne s'exécute qu'une seule fois par session.
        /// </summary>
        public void BeginDay4(bool skipFadeIn = false)
        {
            if (day4Started) return;
            day4Started   = true;
            endingVariant = GameStateManager.Instance?.Day2Choice ?? Day2DoorChoice.None;
            StartCoroutine(Day4Routine(skipFadeIn));
        }

        /// <summary>
        /// Appelé par FinalDoorInteractable quand le joueur active la porte (Fin A uniquement).
        /// </summary>
        public void OnDoorOpened()
        {
            StartCoroutine(FinA_CinematicRoutine());
        }

        // ── Séquence Jour 4 ───────────────────────────────────────────────────

        private IEnumerator Day4Routine(bool skipFadeIn = false)
        {
            Debug.Log($"[Day4] Day4Routine START — skipFadeIn={skipFadeIn} data={data}");
            LockPlayerFull();

            yield return new WaitForSeconds(0.4f);
            Debug.Log("[Day4] After initial pause");

            if (!skipFadeIn)
            {
                Debug.Log("[Day4] Starting FadeIn...");
                yield return StartCoroutine(FadeIn(data != null ? data.FadeDuration : 1.5f));
                Debug.Log("[Day4] FadeIn complete");
            }
            else
            {
                Debug.Log("[Day4] skipFadeIn=true, skipping FadeIn");
            }

            // Ouvrir automatiquement le carnet avec les pensées de fin
            string thoughts = endingVariant == Day2DoorChoice.Opened
                ? (data != null ? data.Thoughts_FinA : "")
                : (data != null ? data.Thoughts_FinB : "");

            JournalManager.Instance?.OpenWithThoughts(thoughts);
            Debug.Log($"[Day4] Journal opened with thoughts='{thoughts}', JournalManager.IsOpen={JournalManager.Instance?.IsOpen}");

            // Attendre que le joueur ferme le carnet
            yield return new WaitUntil(() =>
                JournalManager.Instance == null || !JournalManager.Instance.IsOpen);

            float delay = data != null ? data.DelayAfterJournalClose : 0.5f;
            yield return new WaitForSeconds(delay);

            // Branching selon la fin
            if (endingVariant == Day2DoorChoice.Opened)
                yield return StartCoroutine(FinA_CinematicRoutine());
            else
                yield return StartCoroutine(FinB_Routine());
        }

        // ── Fin A — cinématique (directe après fermeture du carnet) ──────────────

        private IEnumerator FinA_CinematicRoutine()
        {
            LockPlayerFull();
            GameStateManager.Instance?.StartCutscene();

            // Son du blizzard
            if (finBSource != null && data?.SfxDoorBlizzard != null)
                finBSource.PlayOneShot(data.SfxDoorBlizzard);

            // Fondu au blanc — blizzard efface les décors
            float blizzardDuration = data != null ? data.FinA_BlizzardFadeDuration : 2f;
            yield return StartCoroutine(FadeToWhite(blizzardDuration));

            // Son sourd de la porte blindée qui se referme
            if (finBSource != null && data?.SfxDoorClose != null)
                finBSource.PlayOneShot(data.SfxDoorClose);

            float whiteDuration = data != null ? data.FinA_WhiteHoldDuration : 1.5f;
            yield return new WaitForSeconds(whiteDuration);

            // Image de fin Fin A : blanc → image → noir
            yield return StartCoroutine(PlayEndingImage(
                sprite:     data?.EndingImageFinA,
                sfx:        data?.SfxFinA,
                startColor: Color.white));

            TriggerCredits();
        }

        // ── Fin B — séquence complète ─────────────────────────────────────────

        private IEnumerator FinB_Routine()
        {
            LockPlayerFull();

            float delayAlarm = data != null ? data.FinB_DelayBeforeAlarm : 1.5f;
            yield return new WaitForSeconds(delayAlarm);

            // Alarme industrielle
            if (finBSource != null && data?.SfxAlarm != null)
            {
                finBSource.loop   = false;
                finBSource.volume = 1f;
                finBSource.PlayOneShot(data.SfxAlarm);
            }

            float alarmDuration = data != null ? data.FinB_AlarmDuration : 3f;
            yield return new WaitForSeconds(alarmDuration);

            if (finBSource != null)
                finBSource.Stop();

            GameStateManager.Instance?.StartCutscene();

            float lightFade = data != null ? data.FinB_LightFadeDuration : 1.8f;
            yield return StartCoroutine(FadeToWhite(lightFade));

            float whiteHold = data != null ? data.FinB_WhiteHoldDuration : 0.6f;
            yield return new WaitForSeconds(whiteHold);

            yield return StartCoroutine(FadeToBlack(0.25f));
            yield return new WaitForSeconds(0.8f);

            yield return StartCoroutine(PlayEndingImage(
                sprite:     data?.EndingImageFinB,
                sfx:        data?.SfxFinB,
                startColor: Color.black));

            TriggerCredits();
        }

        // ── Utilitaire image de fin ───────────────────────────────────────────

        private IEnumerator PlayEndingImage(Sprite sprite, AudioClip sfx, Color startColor)
        {
            if (EndingPanel.Instance != null && sprite != null)
            {
                bool done = false;
                EndingPanel.Instance.PlayEnding(
                    endSprite:       sprite,
                    sfx:             sfx,
                    startColor:      startColor,
                    fadeInDuration:  data != null ? data.EndingImageFadeInDuration : 2.5f,
                    holdDuration:    data != null ? data.EndingImageHoldDuration   : 5f,
                    fadeOutDuration: data != null ? data.FadeToBlackBeforeCredits  : 2f,
                    onComplete:      () => done = true
                );
                yield return new WaitUntil(() => done);
            }
            else
            {
                yield return new WaitForSeconds(data != null ? data.EndingImageHoldDuration : 5f);
            }
        }

        // ── Utilitaires fondu ─────────────────────────────────────────────────

        private IEnumerator FadeToBlack(float duration)
        {
            bool done = false;
            ScreenFader.Instance?.FadeOut(duration, () => done = true);
            yield return new WaitUntil(() => done || ScreenFader.Instance == null);
        }

        private IEnumerator FadeIn(float duration)
        {
            bool done = false;
            ScreenFader.Instance?.FadeIn(duration, () => done = true);
            yield return new WaitUntil(() => done || ScreenFader.Instance == null);
        }

        private IEnumerator FadeToWhite(float duration)
        {
            bool done = false;
            ScreenFader.Instance?.FadeToWhite(duration, () => done = true);
            yield return new WaitUntil(() => done || ScreenFader.Instance == null);
        }

        // ── Utilitaires joueur ────────────────────────────────────────────────

        private void LockPlayerFull()
        {
            if (playerController != null)
            {
                playerController.CanMove = false;
                playerController.CanLook = false;
            }
            if (interactionSystem != null) interactionSystem.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void UnlockPlayerMovement()
        {
            if (playerController != null)
            {
                playerController.CanMove = true;
                playerController.CanLook = true;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void TriggerCredits()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
