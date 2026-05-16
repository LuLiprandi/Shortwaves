using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shortwaves
{
    /// <summary>
    /// Orchestre la séquence anomalie du Jour 2 :
    ///   1. Radio coupée — le joueur reprend ses mouvements librement.
    ///   2. Pas dans les conduits (audio positionnel qui se déplace quand le joueur s'approche).
    ///   3. Toquements à la porte → prompt de choix (Ouvrir / Ignorer).
    ///   4a. Ouvrir : grincement → blizzard → lampe éteinte → porte refermée → rallumage lampe → parasite radio.
    ///   4b. Ignorer : coups croissants (volume + shake) → bang final → silence absolu → parasite radio.
    ///   5. Journal s'ouvre avec la pensée post-choix.
    ///
    /// Appeler TriggerSequence() depuis JournalManager après la validation du décodage.
    /// </summary>
    public class Day2AnomalySequencer : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Day2ChoiceData data;

        [Header("Références — systèmes")]
        [Tooltip("RadioSystem à couper en début de séquence.")]
        [SerializeField] private RadioSystem radioSystem;

        [Header("Vents — positions audio")]
        [Tooltip("Transforms des bouches d'aération. L'AudioSource se téléporte entre elles.")]
        [SerializeField] private Transform[] ventTransforms;

        [Tooltip("Rayon (m) en deçà duquel le son migre vers la prochaine grille.")]
        [SerializeField] private float ventProximityRadius = 2.5f;

        [Header("Lampe du bureau")]
        [Tooltip("Light à éteindre lors de l'entrée du blizzard (branche Ouvrir).")]
        [SerializeField] private Light deskLamp;

        [Header("Marche vers la porte — branche Ouvrir")]
        [Tooltip("Point cible devant la porte vers lequel le joueur marche. Créer un GameObject vide à 1m devant la porte et l'assigner ici.")]
        [SerializeField] private Transform doorApproachPoint;

        [Tooltip("Distance (m) en deçà de laquelle le fondu au noir se déclenche.")]
        [SerializeField] private float doorApproachDistance = 1.2f;

        [Tooltip("Vitesse de marche scriptée vers la porte (m/s).")]
        [SerializeField] private float doorWalkSpeed = 2.5f;

        [Tooltip("Vitesse de rotation de la caméra vers la porte pendant la marche (degrés/s).")]
        [SerializeField] private float doorTurnSpeed = 120f;

        [Header("Shake caméra — branche Ignorer")]
        [Tooltip("Transform de la caméra à secouer pendant les coups (branche Ignorer). Laisser vide = pas de shake.")]
        [SerializeField] private Transform cameraShakeTarget;

        [Tooltip("Amplitude maximale du shake en unités locales.")]
        [SerializeField] private float shakeAmplitude = 0.04f;

        [Tooltip("Vitesse des oscillations du shake.")]
        [SerializeField] private float shakeFrequency = 18f;

        [Header("Audio Sources")]
        [Tooltip("AudioSource 3D positionnel — pas dans les conduits, blizzard, lampe.")]
        [SerializeField] private AudioSource ambientSource;

        [Tooltip("AudioSource 3D positionnel — toquements à la porte.")]
        [SerializeField] private AudioSource knockingSource;

        [Header("UI — choix")]
        [SerializeField] private Color choicePanelColor = new Color(0f, 0f, 0f, 0.82f);
        [SerializeField] private Color buttonNormalColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        [SerializeField] private Color buttonHoverColor  = new Color(0.25f, 0.22f, 0.16f, 1f);

        // ── État interne ──────────────────────────────────────────────────────

        private bool           sequencePlayed;
        private Day2DoorChoice playerChoice = Day2DoorChoice.None;
        private bool           choiceMade;
        private GameObject     choiceUIRoot;

        private FirstPersonController playerController;
        private InteractionSystem     interactionSystem;
        private Transform             playerTransform;

        // Position locale originale de la caméra avant le shake
        private Vector3 cameraOriginalLocalPos;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            playerController  = FindFirstObjectByType<FirstPersonController>();
            interactionSystem = FindFirstObjectByType<InteractionSystem>();
            if (playerController != null)
                playerTransform = playerController.transform;

            if (cameraShakeTarget != null)
                cameraOriginalLocalPos = cameraShakeTarget.localPosition;
        }

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Déclenche la séquence complète du Jour 2.
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
            // Couper la radio immédiatement
            radioSystem?.SetActive(false);

            // Courte respiration avant les sons atmosphériques
            yield return new WaitForSeconds(0.8f);

            // Phase 1 : pas dans les conduits (joueur libre)
            yield return StartCoroutine(VentFootstepsPhase());

            // Phase 2 : toquements + choix (joueur immobilisé)
            yield return StartCoroutine(DoorKnockingPhase());

            // Phase 3 : exécution du choix
            if (playerChoice == Day2DoorChoice.Opened)
                yield return StartCoroutine(BranchOpen());
            else
                yield return StartCoroutine(BranchIgnore());

            // Sauvegarder le choix dans GameStateManager (persisté)
            GameStateManager.Instance?.SetDay2Choice(playerChoice);

            // Délai puis ouverture automatique du journal
            yield return new WaitForSeconds(data.DelayBeforeJournal);

            string thoughts = playerChoice == Day2DoorChoice.Opened
                ? data.PostAnomalyThoughts_Opened
                : data.PostAnomalyThoughts_Ignored;

            JournalManager.Instance?.OpenWithThoughts(thoughts);
        }

        // ── Phase 1 : Pas dans les conduits ──────────────────────────────────

        private IEnumerator VentFootstepsPhase()
        {
            // Le joueur peut se déplacer librement pour approcher les grilles
            SetPlayerMovement(canMove: true, lockInteractions: false);

            if (data.SfxFootstepsVents == null || ventTransforms == null || ventTransforms.Length == 0)
            {
                yield return new WaitForSeconds(data.FootstepsDuration);
                yield break;
            }

            int currentVent = 0;
            PositionAmbientAt(ventTransforms[currentVent]);
            ambientSource.clip   = data.SfxFootstepsVents;
            ambientSource.loop   = true;
            ambientSource.volume = 1f;
            ambientSource.Play();

            float elapsed = 0f;
            while (elapsed < data.FootstepsDuration)
            {
                elapsed += Time.deltaTime;

                // Le son migre vers la prochaine grille quand le joueur s'approche trop
                if (playerTransform != null && ventTransforms.Length > 1)
                {
                    float dist = Vector3.Distance(playerTransform.position,
                        ventTransforms[currentVent].position);

                    if (dist < ventProximityRadius)
                    {
                        currentVent = (currentVent + 1) % ventTransforms.Length;
                        PositionAmbientAt(ventTransforms[currentVent]);
                    }
                }

                yield return null;
            }

            ambientSource.Stop();

            // Immobiliser le joueur pour la tension des toquements
            SetPlayerMovement(canMove: false, lockInteractions: true);
        }

        // ── Phase 2 : Toquements + choix ─────────────────────────────────────

        private IEnumerator DoorKnockingPhase()
        {
            // Premiers toquements discrets en boucle
            if (knockingSource != null && data.SfxKnocking != null)
            {
                knockingSource.clip   = data.SfxKnocking;
                knockingSource.loop   = true;
                knockingSource.volume = 0.6f;
                knockingSource.Play();
            }

            // Monter progressivement le volume pendant la phase de tension
            float elapsed = 0f;
            while (elapsed < data.KnockingDuration)
            {
                elapsed += Time.deltaTime;
                if (knockingSource != null)
                    knockingSource.volume = Mathf.Lerp(0.6f, 1f, elapsed / data.KnockingDuration);
                yield return null;
            }

            // Afficher le prompt de choix et attendre la décision
            choiceMade = false;
            ShowChoiceUI();
            yield return new WaitUntil(() => choiceMade);

            if (knockingSource != null) knockingSource.Stop();
        }

        // ── Branche A : Ouvrir ────────────────────────────────────────────────

        private IEnumerator BranchOpen()
        {
            // Si le joueur est loin de la porte, il marche vers elle avant le fondu
            yield return StartCoroutine(WalkToDoor());

            // Fondu au noir
            bool fadeReady = false;
            ScreenFader.Instance?.FadeOut(0.6f, () => fadeReady = true);
            yield return new WaitUntil(() => fadeReady || ScreenFader.Instance == null);

            // Grincement de la porte sous le noir
            if (ambientSource != null && data.SfxDoorCreak != null)
            {
                ambientSource.loop = false;
                ambientSource.PlayOneShot(data.SfxDoorCreak);
                yield return new WaitForSeconds(data.SfxDoorCreak.length);
            }

            // Blizzard qui s'engouffre + lampe qui s'éteint (toujours sous le noir)
            if (ambientSource != null && data.SfxBlizzardGust != null)
            {
                ambientSource.clip   = data.SfxBlizzardGust;
                ambientSource.loop   = false;
                ambientSource.volume = 1f;
                ambientSource.Play();
            }

            if (deskLamp != null) deskLamp.enabled = false;

            yield return new WaitForSeconds(data.BlizzardGustDuration);
            if (ambientSource != null) ambientSource.Stop();

            // Silence pesant — porte refermée
            yield return new WaitForSeconds(0.6f);

            // Lampe se rallume, retour à la vue
            if (deskLamp != null) deskLamp.enabled = true;
            if (ambientSource != null && data.SfxLampRelight != null)
                ambientSource.PlayOneShot(data.SfxLampRelight);

            bool fadeInReady = false;
            ScreenFader.Instance?.FadeIn(0.8f, () => fadeInReady = true);
            yield return new WaitUntil(() => fadeInReady || ScreenFader.Instance == null);

            yield return new WaitForSeconds(0.4f);

            // Radio grésille puis se coupe définitivement
            yield return StartCoroutine(RadioStaticAndCut());
        }

        // ── Branche B : Ignorer ───────────────────────────────────────────────

        private IEnumerator BranchIgnore()
        {
            // Coups de plus en plus forts avec shake de caméra croissant
            for (int i = 0; i < data.HeavyKnockCount; i++)
            {
                float intensity = 1f + i * 0.2f;

                if (knockingSource != null && data.SfxKnocking != null)
                    knockingSource.PlayOneShot(data.SfxKnocking, Mathf.Min(intensity, 1.4f));

                // Shake proportionnel à l'intensité du coup
                float shakeDuration = Mathf.Min(data.BangInterval * 0.6f, 0.5f);
                yield return StartCoroutine(ShakeCamera(shakeDuration, shakeAmplitude * intensity));

                float remaining = data.BangInterval - shakeDuration;
                if (remaining > 0f)
                    yield return new WaitForSeconds(remaining);
            }

            // Grand bang final — shake violent
            if (data.SfxFinalBang != null)
            {
                if (knockingSource != null)
                    knockingSource.PlayOneShot(data.SfxFinalBang, 1.4f);
                float bangShakeDuration = Mathf.Min(data.SfxFinalBang.length * 0.7f, 0.8f);
                yield return StartCoroutine(ShakeCamera(bangShakeDuration, shakeAmplitude * 2.5f));
                float rest = data.SfxFinalBang.length - bangShakeDuration + 0.3f;
                if (rest > 0f) yield return new WaitForSeconds(rest);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            // Silence absolu
            if (knockingSource != null) knockingSource.Stop();
            yield return new WaitForSeconds(1.0f);

            // Radio grésille puis se coupe
            yield return StartCoroutine(RadioStaticAndCut());
        }

        // ── Utilitaires séquence ──────────────────────────────────────────────

        /// <summary>
        /// Si le joueur est plus loin que <see cref="doorApproachDistance"/> de <see cref="doorApproachPoint"/>,
        /// le fait marcher vers la porte et pivoter progressivement dans sa direction.
        /// Se termine dès que le joueur est assez proche, ou immédiatement si doorApproachPoint n'est pas assigné.
        /// </summary>
        private IEnumerator WalkToDoor()
        {
            if (doorApproachPoint == null || playerTransform == null) yield break;

            Vector3 target = doorApproachPoint.position;
            // Ignorer la hauteur — on ne veut pas que le joueur flotte
            target.y = playerTransform.position.y;

            float dist = Vector3.Distance(playerTransform.position, target);
            if (dist <= doorApproachDistance) yield break;

            // Verrouiller les inputs joueur mais garder le CharacterController actif
            playerController.CanMove = false;
            playerController.CanLook = false;

            CharacterController cc = playerController.CharacterController;

            while (true)
            {
                target.y = playerTransform.position.y;
                dist = Vector3.Distance(playerTransform.position, target);
                if (dist <= doorApproachDistance) break;

                // Direction vers la porte (plan horizontal)
                Vector3 dir = (target - playerTransform.position).normalized;

                // Rotation progressive du corps vers la porte
                Quaternion targetBodyRot = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
                playerTransform.rotation = Quaternion.RotateTowards(
                    playerTransform.rotation, targetBodyRot, doorTurnSpeed * Time.deltaTime);

                // Rotation progressive de la caméra (pitch → 0 progressivement)
                Transform cam = playerController.PlayerCamera;
                if (cam != null)
                    cam.localRotation = Quaternion.RotateTowards(
                        cam.localRotation, Quaternion.identity, doorTurnSpeed * Time.deltaTime);

                // Déplacement
                Vector3 move = new Vector3(0f, Physics.gravity.y, 0f);
                if (cc != null && cc.enabled)
                    cc.Move((dir * doorWalkSpeed + move) * Time.deltaTime);

                yield return null;
            }

            // Snap final propre
            playerController.CanMove = false; // reste verrouillé pour la suite
            playerController.CanLook = false;
        }

        private IEnumerator RadioStaticAndCut()
        {
            if (ambientSource != null && data.SfxRadioStatic != null)
            {
                ambientSource.PlayOneShot(data.SfxRadioStatic);
                yield return new WaitForSeconds(data.SfxRadioStatic.length);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>Secoue cameraShakeTarget pendant <paramref name="duration"/> secondes.</summary>
        private IEnumerator ShakeCamera(float duration, float amplitude)
        {
            if (cameraShakeTarget == null) { yield return new WaitForSeconds(duration); yield break; }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float fade     = 1f - progress;               // décroît en fin de shake
                float offsetX  = Mathf.Sin(elapsed * shakeFrequency)       * amplitude * fade;
                float offsetY  = Mathf.Sin(elapsed * shakeFrequency * 1.3f) * amplitude * fade;
                cameraShakeTarget.localPosition = cameraOriginalLocalPos + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            cameraShakeTarget.localPosition = cameraOriginalLocalPos;
        }

        private void PositionAmbientAt(Transform target)
        {
            if (ambientSource != null && target != null)
                ambientSource.transform.position = target.position;
        }

        /// <summary>
        /// Active/désactive le mouvement et les interactions du joueur.
        /// <paramref name="lockInteractions"/> permet de garder les interactions actives pendant la phase des pas.
        /// </summary>
        private void SetPlayerMovement(bool canMove, bool lockInteractions)
        {
            if (playerController  != null) playerController.CanMove  = canMove;
            if (interactionSystem != null && lockInteractions)
                interactionSystem.enabled = canMove;
        }

        // ── UI — prompt de choix ──────────────────────────────────────────────

        private void ShowChoiceUI()
        {
            choiceUIRoot = new GameObject("Day2_ChoiceUI");

            var canvas          = choiceUIRoot.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            choiceUIRoot.AddComponent<CanvasScaler>();
            choiceUIRoot.AddComponent<GraphicRaycaster>();

            // Fond semi-transparent
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(choiceUIRoot.transform, false);
            var bgRT       = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            bgGO.AddComponent<Image>().color = choicePanelColor;

            // Texte du prompt — centré verticalement dans la moitié haute
            var promptGO = new GameObject("PromptText");
            promptGO.transform.SetParent(choiceUIRoot.transform, false);
            var promptRT       = promptGO.AddComponent<RectTransform>();
            promptRT.anchorMin = new Vector2(0.15f, 0.52f);
            promptRT.anchorMax = new Vector2(0.85f, 0.82f);
            promptRT.offsetMin = promptRT.offsetMax = Vector2.zero;
            var promptTMP              = promptGO.AddComponent<TextMeshProUGUI>();
            promptTMP.text             = data?.ChoicePromptText ?? "";
            promptTMP.fontSize         = 22f;
            promptTMP.color            = Color.white;
            promptTMP.alignment        = TextAlignmentOptions.Center;
            promptTMP.enableWordWrapping = true;

            // Bouton "Ouvrir" (gauche)
            CreateChoiceButton(choiceUIRoot.transform,
                data?.ButtonLabelOpen   ?? "Ouvrir",
                new Vector2(0.15f, 0.30f), new Vector2(0.45f, 0.48f),
                () => OnChoiceMade(Day2DoorChoice.Opened));

            // Bouton "Ignorer" (droite)
            CreateChoiceButton(choiceUIRoot.transform,
                data?.ButtonLabelIgnore ?? "Ignorer",
                new Vector2(0.55f, 0.30f), new Vector2(0.85f, 0.48f),
                () => OnChoiceMade(Day2DoorChoice.Ignored));

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void CreateChoiceButton(Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            var btnGO = new GameObject("Btn_" + label);
            btnGO.transform.SetParent(parent, false);

            var rt       = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img   = btnGO.AddComponent<Image>();
            img.color = buttonNormalColor;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors              = btn.colors;
            colors.normalColor      = buttonNormalColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor     = new Color(0.08f, 0.08f, 0.08f, 1f);
            btn.colors              = colors;

            btn.onClick.AddListener(() => onClick?.Invoke());

            // Libellé centré
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);
            var lblRT       = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            var tmp       = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 20f;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private void OnChoiceMade(Day2DoorChoice choice)
        {
            playerChoice = choice;
            choiceMade   = true;

            if (choiceUIRoot != null)
            {
                choiceUIRoot.SetActive(false);
                Destroy(choiceUIRoot, 0.1f);
                choiceUIRoot = null;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}
