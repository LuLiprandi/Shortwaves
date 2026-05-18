using System.Collections;
using UnityEngine;

namespace Shortwaves
{
    public class Day4EndingSequencer : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Day4Data data;

        [Header("Fin B — audio")]
        [Tooltip("AudioSource dédiée aux sons séquentiels de Fin B (alarme, pas, voix).")]
        [SerializeField] private AudioSource finBSource;

        private bool                  day4Started;
        private Day2DoorChoice        endingVariant;
        private FirstPersonController playerController;
        private InteractionSystem     interactionSystem;

        private void Awake()
        {
            playerController  = FindFirstObjectByType<FirstPersonController>();
            interactionSystem = FindFirstObjectByType<InteractionSystem>();
        }

        public void BeginDay4(bool skipFadeIn = false)
        {
            if (day4Started) return;
            day4Started   = true;
            endingVariant = GameStateManager.Instance?.Day2Choice ?? Day2DoorChoice.None;
            StartCoroutine(Day4Routine(skipFadeIn));
        }

        private IEnumerator Day4Routine(bool skipFadeIn)
        {
            LockPlayer();

            yield return new WaitForSeconds(0.4f);

            if (!skipFadeIn)
                yield return StartCoroutine(FadeIn(data != null ? data.FadeDuration : 1.5f));

            string thoughts = endingVariant == Day2DoorChoice.Opened
                ? (data != null ? data.Thoughts_FinA : "")
                : (data != null ? data.Thoughts_FinB : "");

            JournalManager.Instance?.OpenWithThoughts(thoughts);

            yield return new WaitUntil(() =>
                JournalManager.Instance == null || !JournalManager.Instance.IsOpen);

            yield return new WaitForSeconds(data != null ? data.DelayAfterJournalClose : 0.5f);

            yield return StartCoroutine(FadeToBlack(1.5f));

            if (endingVariant != Day2DoorChoice.Opened && finBSource != null && data?.SfxAlarm != null)
            {
                finBSource.PlayOneShot(data.SfxAlarm);
                yield return new WaitForSeconds(data != null ? data.FinB_AlarmDuration : 3f);
            }

            Texture2D endingTex = endingVariant == Day2DoorChoice.Opened
                ? data?.EndingImageFinA
                : data?.EndingImageFinB;

            AudioClip endingSfx = endingVariant == Day2DoorChoice.Opened
                ? data?.SfxFinA
                : data?.SfxFinB;

            float fadeInDuration = data != null ? data.EndingImageFadeInDuration : 2.5f;

            bool done = false;
            if (EndingPanel.Instance != null)
            {
                EndingPanel.Instance.ShowImage(endingTex, endingSfx, fadeInDuration, () => done = true);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;

                yield return new WaitUntil(() => done);
            }

            GoToMainMenu();
        }

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

        private void LockPlayer()
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

        private void GoToMainMenu()
        {
            EndingPanel.Instance?.Hide();
            ScreenFader.Instance?.SetClear();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
